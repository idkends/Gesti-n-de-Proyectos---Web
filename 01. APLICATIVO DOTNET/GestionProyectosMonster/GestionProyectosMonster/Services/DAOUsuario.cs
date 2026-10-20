using GestionProyectosMonster.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestionProyectosMonster.Services;

public class DAOUsuario
{
    public const string PERFIL_ADMINISTRADOR     = "PERF0001";
    public const string PERFIL_RRHH              = "PERF0002";
    public const string PERFIL_JEFE_DEPARTAMENTO = "PERF0003";
    public const string PERFIL_EMPLEADO          = "PERF0004";

    public static readonly (string Codigo, string Descripcion)[] PerfilesDisponibles =
    [
        (PERFIL_ADMINISTRADOR,     Usuario.ROL_ADMINISTRADOR),
        (PERFIL_RRHH,              Usuario.ROL_RRHH),
        (PERFIL_JEFE_DEPARTAMENTO, Usuario.ROL_JEFE_DEPARTAMENTO),
        (PERFIL_EMPLEADO,          Usuario.ROL_EMPLEADO)
    ];

    private readonly IMongoDatabase _db;
    private IMongoCollection<BsonDocument> Usuarios => _db.GetCollection<BsonDocument>("usuarios");
    private IMongoCollection<BsonDocument> Empleados => _db.GetCollection<BsonDocument>("empleados");
    private IMongoCollection<BsonDocument> Perfiles => _db.GetCollection<BsonDocument>("perfiles");
    private IMongoCollection<BsonDocument> EstadosUsuario => _db.GetCollection<BsonDocument>("estadosUsuario");

    public DAOUsuario(IMongoDatabase db) => _db = db;

    public Usuario? Autenticar(string codigoEmpleado, string passwordPlano)
    {
        string codigo = codigoEmpleado.Trim().ToUpper();
        var u = Usuarios.Find(Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("_id", codigo),
            Builders<BsonDocument>.Filter.Eq("estadoCodigo", "A"))).FirstOrDefault();
        if (u == null) return null;
        string hash = u["passwordHash"].AsString;
        if (!Encriptador.Verificar(passwordPlano, hash)) return null;
        return Mapear(u);
    }

    public List<Usuario> Listar()
    {
        return Usuarios.Find(FilterDefinition<BsonDocument>.Empty)
            .SortBy(u => u["_id"])
            .ToList()
            .Select(Mapear)
            .ToList();
    }

    public bool Insertar(Usuario usuario, string passwordPlano)
    {
        string? error = PoliticaContrasena.Validar(passwordPlano);
        if (error != null) throw new ArgumentException(error);

        string hash = Encriptador.Hash(passwordPlano);
        string perfil = Limpiar(usuario.PerfilCodigo, PERFIL_EMPLEADO);
        string codigo = usuario.CodigoEmpleado.Trim().ToUpper();

        var doc = new BsonDocument
        {
            ["_id"] = codigo,
            ["passwordHash"] = hash,
            ["estadoCodigo"] = Limpiar(usuario.EstadoCodigo, "A"),
            ["fechaCreacion"] = DateTime.Now,
            ["fechaModificacion"] = DateTime.Now,
            ["pieFirma"] = Limpiar(usuario.PieFirma, "Usuario creado"),
            ["perfilesAsignados"] = new BsonArray
            {
                new BsonDocument
                {
                    ["perfilCodigo"] = perfil,
                    ["fechaAsignacion"] = DateTime.Today,
                    ["fechaRetiro"] = BsonNull.Value
                }
            }
        };
        Usuarios.InsertOne(doc);
        return true;
    }

    public bool CambiarEstado(string codigoEmpleado, string estado)
    {
        var result = Usuarios.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", codigoEmpleado.Trim().ToUpper()),
            Builders<BsonDocument>.Update
                .Set("estadoCodigo", Limpiar(estado, "A"))
                .Set("fechaModificacion", DateTime.Now));
        return result.ModifiedCount > 0;
    }

    public bool CambiarPerfil(string codigoEmpleado, string perfilCodigo)
    {
        string codigo = codigoEmpleado.Trim().ToUpper();
        string perfil = Limpiar(perfilCodigo, PERFIL_EMPLEADO);
        var u = Usuarios.Find(Builders<BsonDocument>.Filter.Eq("_id", codigo)).FirstOrDefault();
        if (u == null) return false;

        string? perfilActual = ObtenerPerfilActual(u);
        if (perfil == perfilActual) return true;

        CerrarPerfilesActivos(codigo);
        AgregarPerfil(codigo, perfil);
        return true;
    }

    public bool ExisteEmpleado(string codigoEmpleado) =>
        Empleados.Find(Builders<BsonDocument>.Filter.Eq("_id", codigoEmpleado.Trim().ToUpper())).Any();

    public bool ExisteUsuario(string codigoEmpleado) =>
        Usuarios.Find(Builders<BsonDocument>.Filter.Eq("_id", codigoEmpleado.Trim().ToUpper())).Any();

    public bool EsPerfilValido(string? perfilCodigo) =>
        PerfilesDisponibles.Any(p => p.Codigo == perfilCodigo);

    public bool CambiarPassword(string codigoEmpleado, string nuevaPasswordPlana)
    {
        string? error = PoliticaContrasena.Validar(nuevaPasswordPlana);
        if (error != null) throw new ArgumentException(error);

        string codigo = codigoEmpleado.Trim().ToUpper();
        string nuevoHash = Encriptador.Hash(nuevaPasswordPlana);

        var u = Usuarios.Find(Builders<BsonDocument>.Filter.Eq("_id", codigo)).FirstOrDefault();
        if (u == null) return false;

        string? perfilActual = ObtenerPerfilActual(u);
        string perfil = Limpiar(perfilActual, PERFIL_EMPLEADO);

        var result = Usuarios.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", codigo),
            Builders<BsonDocument>.Update
                .Set("passwordHash", nuevoHash)
                .Set("fechaModificacion", DateTime.Now)
                .Set("perfilesAsignados", new BsonArray
                {
                    new BsonDocument
                    {
                        ["perfilCodigo"] = perfil,
                        ["fechaAsignacion"] = DateTime.Today,
                        ["fechaRetiro"] = BsonNull.Value
                    }
                }));
        return result.ModifiedCount > 0;
    }

    public bool CrearPerfilBase(string codigoEmpleado, string passwordPlana)
    {
        string codigo = codigoEmpleado.Trim().ToUpper();
        if (ExisteUsuario(codigo)) return false; // idempotente

        var usuario = new Usuario
        {
            CodigoEmpleado = codigo,
            PerfilCodigo   = PERFIL_EMPLEADO,
            EstadoCodigo   = "A",
            PieFirma       = "Creado automáticamente"
        };
        return Insertar(usuario, passwordPlana);
    }

    // ==================== PRIVADOS ====================

    private static string? ObtenerPerfilActual(BsonDocument u)
    {
        if (!u.Contains("perfilesAsignados")) return null;
        var activos = u["perfilesAsignados"].AsBsonArray
            .Select(x => x.AsBsonDocument)
            .Where(p => !p.Contains("fechaRetiro") || p["fechaRetiro"].IsBsonNull)
            .OrderByDescending(p => p["fechaAsignacion"].ToUniversalTime())
            .ToList();
        return activos.FirstOrDefault()?["perfilCodigo"].AsString;
    }

    private void CerrarPerfilesActivos(string codigo)
    {
        var u = Usuarios.Find(Builders<BsonDocument>.Filter.Eq("_id", codigo)).FirstOrDefault();
        if (u == null || !u.Contains("perfilesAsignados")) return;
        var arr = u["perfilesAsignados"].AsBsonArray;
        foreach (var item in arr)
        {
            var p = item.AsBsonDocument;
            if (!p.Contains("fechaRetiro") || p["fechaRetiro"].IsBsonNull)
                p["fechaRetiro"] = DateTime.Today;
        }
        Usuarios.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", codigo),
            Builders<BsonDocument>.Update.Set("perfilesAsignados", arr));
    }

    private void AgregarPerfil(string codigo, string perfil)
    {
        var item = new BsonDocument
        {
            ["perfilCodigo"] = perfil,
            ["fechaAsignacion"] = DateTime.Today,
            ["fechaRetiro"] = BsonNull.Value
        };
        Usuarios.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", codigo),
            Builders<BsonDocument>.Update.Push("perfilesAsignados", item));
    }

    private Usuario Mapear(BsonDocument u)
    {
        string codigo = u["_id"].AsString;
        var emp = Empleados.Find(Builders<BsonDocument>.Filter.Eq("_id", codigo)).FirstOrDefault();
        string nombreEmpleado = emp != null ? $"{emp["nombre"].AsString} {emp["apellido"].AsString}" : "";
        string? depCodigo = emp != null ? LeerStr(emp, "departamentoCodigo") : null;
        string? carCodigo = emp != null ? LeerStr(emp, "cargoCodigo") : null;
        string? foto = emp != null ? LeerStr(emp, "fotoRuta") : null;

        string? perfilCodigo = ObtenerPerfilActual(u);
        var perfilDoc = perfilCodigo != null
            ? Perfiles.Find(Builders<BsonDocument>.Filter.Eq("_id", perfilCodigo)).FirstOrDefault()
            : null;
        string? perfilDescripcion = perfilDoc?["descripcion"].AsString;

        var estadoDoc = EstadosUsuario.Find(Builders<BsonDocument>.Filter.Eq("_id", u["estadoCodigo"].AsString)).FirstOrDefault();
        string? estadoDescripcion = estadoDoc?["descripcion"].AsString;

        var usuario = new Usuario
        {
            CodigoEmpleado = codigo,
            PasswordHash = u["passwordHash"].AsString,
            EstadoCodigo = u["estadoCodigo"].AsString,
            FechaCreacion = u["fechaCreacion"].ToUniversalTime(),
            FechaModificacion = u["fechaModificacion"].ToUniversalTime(),
            PieFirma = LeerStr(u, "pieFirma") ?? "",
            NombreEmpleado = nombreEmpleado,
            FotoRuta = foto,
            DepartamentoCodigo = depCodigo,
            CargoCodigo = carCodigo,
            EstadoDescripcion = estadoDescripcion,
            PerfilCodigo = perfilCodigo,
            PerfilDescripcion = perfilDescripcion
        };
        usuario.RolUsuario = ResolverRol(usuario);
        return usuario;
    }

    private static string ResolverRol(Usuario u)
    {
        string desc = (u.PerfilDescripcion ?? "").ToLower();
        if (desc.Contains("administrador")) return Usuario.ROL_ADMINISTRADOR;
        if (desc.Contains("recursos humanos") || desc.Contains("rrhh")) return Usuario.ROL_RRHH;
        if (desc.Contains("jefe")) return Usuario.ROL_JEFE_DEPARTAMENTO;
        if (desc.Contains("empleado")) return Usuario.ROL_EMPLEADO;
        if (u.CargoCodigo == "ADM") return Usuario.ROL_ADMINISTRADOR;
        if (u.DepartamentoCodigo == "002") return Usuario.ROL_RRHH;
        if (u.CargoCodigo == "JPR") return Usuario.ROL_JEFE_DEPARTAMENTO;
        return u.PerfilCodigo switch
        {
            PERFIL_ADMINISTRADOR     => Usuario.ROL_ADMINISTRADOR,
            PERFIL_RRHH              => Usuario.ROL_RRHH,
            PERFIL_JEFE_DEPARTAMENTO => Usuario.ROL_JEFE_DEPARTAMENTO,
            _                        => Usuario.ROL_EMPLEADO
        };
    }

    private static string? LeerStr(BsonDocument doc, string campo) =>
        doc.Contains(campo) && !doc[campo].IsBsonNull ? doc[campo].AsString : null;

    private static string Limpiar(string? valor, string defecto) =>
        string.IsNullOrWhiteSpace(valor) ? defecto : valor.Trim();
}
