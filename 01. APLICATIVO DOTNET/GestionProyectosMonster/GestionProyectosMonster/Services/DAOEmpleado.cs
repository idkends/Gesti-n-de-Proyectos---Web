using GestionProyectosMonster.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestionProyectosMonster.Services;

public class DAOEmpleado
{
    private readonly IMongoDatabase _db;
    private IMongoCollection<BsonDocument> Empleados => _db.GetCollection<BsonDocument>("empleados");
    private IMongoCollection<BsonDocument> Departamentos => _db.GetCollection<BsonDocument>("departamentos");
    private IMongoCollection<BsonDocument> EstadosCiviles => _db.GetCollection<BsonDocument>("estadosCiviles");
    private IMongoCollection<BsonDocument> Parentescos => _db.GetCollection<BsonDocument>("parentescos");
    private IMongoCollection<BsonDocument> Counters => _db.GetCollection<BsonDocument>("counters");

    public DAOEmpleado(IMongoDatabase db) => _db = db;

    // ==================== EMPLEADOS ====================

    private static readonly Dictionary<string, string> Sexos = new()
    {
        ["M"] = "Masculino", ["F"] = "Femenino", ["H"] = "Hermafrodita"
    };

    public string GenerarCodigoSiguiente()
    {
        int seq = SiguienteSecuencia("empleado");
        return $"EMP{seq:D3}";
    }

    private int SiguienteSecuencia(string nombre)
    {
        var filtro = Builders<BsonDocument>.Filter.Eq("_id", nombre);
        var update = Builders<BsonDocument>.Update.Inc("seq", 1);
        var opciones = new FindOneAndUpdateOptions<BsonDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };
        var doc = Counters.FindOneAndUpdate(filtro, update, opciones);
        return doc["seq"].ToInt32();
    }

    public List<Empleado> Listar()
    {
        var dictDep = Departamentos.Find(FilterDefinition<BsonDocument>.Empty)
            .ToList()
            .ToDictionary(d => d["_id"].AsString, d => d);

        var lista = Empleados.Find(FilterDefinition<BsonDocument>.Empty)
            .SortBy(e => e["_id"])
            .ToList()
            .Select(e => Mapear(e, dictDep))
            .ToList();
        return lista;
    }

    public Dictionary<string, Dictionary<string, List<Empleado>>> ListarOrganigrama()
    {
        var resultado = new Dictionary<string, Dictionary<string, List<Empleado>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in Listar())
        {
            string dept  = string.IsNullOrWhiteSpace(e.DepartamentoDescripcion) ? "Sin departamento" : e.DepartamentoDescripcion;
            string cargo = string.IsNullOrWhiteSpace(e.CargoDescripcion)         ? "Sin cargo"         : e.CargoDescripcion;
            if (!resultado.ContainsKey(dept))
                resultado[dept] = new Dictionary<string, List<Empleado>>(StringComparer.OrdinalIgnoreCase);
            if (!resultado[dept].ContainsKey(cargo))
                resultado[dept][cargo] = new List<Empleado>();
            resultado[dept][cargo].Add(e);
        }
        return resultado;
    }

    public Empleado? ObtenerPorCodigo(string codigo)
    {
        var doc = Empleados.Find(Builders<BsonDocument>.Filter.Eq("_id", codigo.Trim().ToUpper())).FirstOrDefault();
        if (doc == null) return null;
        var dictDep = Departamentos.Find(FilterDefinition<BsonDocument>.Empty)
            .ToList().ToDictionary(d => d["_id"].AsString, d => d);
        return Mapear(doc, dictDep);
    }

    public bool Existe(string codigo) => ObtenerPorCodigo(codigo) != null;

    public void Insertar(Empleado e)
    {
        var doc = new BsonDocument
        {
            ["_id"] = e.Codigo,
            ["nombre"] = e.Nombre,
            ["apellido"] = e.Apellido,
            ["cedula"] = e.Cedula,
            ["email"] = e.Email,
            ["telefono"] = e.Telefono,
            ["departamentoCodigo"] = e.DepartamentoCodigo,
            ["cargoCodigo"] = e.CargoCodigo,
            ["sexoCodigo"] = e.SexoCodigo,
            ["estadoCivilCodigo"] = Nullable(e.EstadoCivilCodigo),
            ["fechaNacimiento"] = e.FechaNacimiento.Date,
            ["fechaSalida"] = e.FechaSalida.Date,
            ["direccion"] = e.Direccion,
            ["pasaporte"] = string.IsNullOrWhiteSpace(e.Pasaporte) ? "N/A" : e.Pasaporte,
            ["cargosFamiliares"] = e.CargosFamiliares,
            ["fotoRuta"] = Nullable(e.FotoRuta),
            ["educacion"] = new BsonArray(),
            ["familiares"] = new BsonArray()
        };
        Empleados.InsertOne(doc);
    }

    public void Actualizar(Empleado e)
    {
        var update = Builders<BsonDocument>.Update
            .Set("departamentoCodigo", e.DepartamentoCodigo)
            .Set("cargoCodigo", e.CargoCodigo)
            .Set("nombre", e.Nombre)
            .Set("apellido", e.Apellido)
            .Set("sexoCodigo", e.SexoCodigo)
            .Set("estadoCivilCodigo", Nullable(e.EstadoCivilCodigo))
            .Set("fechaNacimiento", e.FechaNacimiento.Date)
            .Set("fechaSalida", e.FechaSalida.Date)
            .Set("direccion", e.Direccion)
            .Set("telefono", e.Telefono)
            .Set("email", e.Email)
            .Set("cedula", e.Cedula)
            .Set("pasaporte", string.IsNullOrWhiteSpace(e.Pasaporte) ? "N/A" : e.Pasaporte)
            .Set("fotoRuta", Nullable(e.FotoRuta));
        Empleados.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", e.Codigo), update);
    }

    public void ActualizarFoto(string codigoEmpleado, string rutaFoto)
    {
        Empleados.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", codigoEmpleado),
            Builders<BsonDocument>.Update.Set("fotoRuta", rutaFoto));
    }

    // ==================== EDUCACIÓN ====================

    public string GenerarCodigoEducacion()
    {
        int seq = SiguienteSecuencia("educacion");
        return $"EDU{seq:D3}";
    }

    public List<Educacion> ListarEducacion(string codigoEmpleado)
    {
        var doc = Empleados.Find(Builders<BsonDocument>.Filter.Eq("_id", codigoEmpleado)).FirstOrDefault();
        if (doc == null || !doc.Contains("educacion")) return [];
        var lista = doc["educacion"].AsBsonArray
            .Select(x => MapearEducacion(x.AsBsonDocument, codigoEmpleado))
            .OrderByDescending(x => x.FechaGrado)
            .ToList();
        return lista;
    }

    public void InsertarEducacion(Educacion edu)
    {
        var item = new BsonDocument
        {
            ["codigo"] = edu.Codigo,
            ["titulo"] = edu.Titulo,
            ["institucion"] = edu.Institucion,
            ["fechaGrado"] = edu.FechaGrado.Date,
            ["fechaInicio"] = edu.FechaInicio.Date
        };
        Empleados.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", edu.CodigoEmpleado),
            Builders<BsonDocument>.Update.Push("educacion", item));
    }

    public void ActualizarEducacion(Educacion edu)
    {
        var filtro = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("_id", edu.CodigoEmpleado),
            Builders<BsonDocument>.Filter.Eq("educacion.codigo", edu.Codigo));
        var update = Builders<BsonDocument>.Update
            .Set("educacion.$.titulo", edu.Titulo)
            .Set("educacion.$.institucion", edu.Institucion)
            .Set("educacion.$.fechaGrado", edu.FechaGrado.Date)
            .Set("educacion.$.fechaInicio", edu.FechaInicio.Date);
        Empleados.UpdateOne(filtro, update);
    }

    public void EliminarEducacion(string codigoEducacion)
    {
        var filtro = Builders<BsonDocument>.Filter.Eq("educacion.codigo", codigoEducacion);
        var update = Builders<BsonDocument>.Update.PullFilter("educacion",
            Builders<BsonDocument>.Filter.Eq("codigo", codigoEducacion));
        Empleados.UpdateOne(filtro, update);
    }

    // ==================== FAMILIARES ====================

    public string GenerarCodigoFamiliar()
    {
        int seq = SiguienteSecuencia("familiar");
        return $"FAM{seq:D3}";
    }

    public List<Familiar> ListarFamiliares(string codigoEmpleado)
    {
        var doc = Empleados.Find(Builders<BsonDocument>.Filter.Eq("_id", codigoEmpleado)).FirstOrDefault();
        if (doc == null || !doc.Contains("familiares")) return [];

        var dictPar = Parentescos.Find(FilterDefinition<BsonDocument>.Empty)
            .ToList().ToDictionary(p => p["_id"].AsString, p => p["descripcion"].AsString);

        var lista = doc["familiares"].AsBsonArray
            .Select(x => x.AsBsonDocument)
            .OrderByDescending(f => f.Contains("fechaActualizacion") ? f["fechaActualizacion"].ToUniversalTime() : DateTime.MinValue)
            .Select(f => new Familiar
            {
                Codigo = f["codigo"].AsString,
                CodigoEmpleado = codigoEmpleado,
                ParentescoCodigo = f["parentescoCodigo"].AsString,
                SexoCodigo = f["sexoCodigo"].AsString,
                Nombre = f["nombre"].AsString,
                Apellido = f["apellido"].AsString,
                FechaNacimiento = f["fechaNacimiento"].ToUniversalTime(),
                ParentescoDescripcion = dictPar.TryGetValue(f["parentescoCodigo"].AsString, out var pd) ? pd : null,
                SexoDescripcion = Sexos.TryGetValue(f["sexoCodigo"].AsString, out var sd) ? sd : null
            })
            .ToList();
        return lista;
    }

    public void InsertarFamiliar(Familiar fam)
    {
        var item = new BsonDocument
        {
            ["codigo"] = fam.Codigo,
            ["parentescoCodigo"] = fam.ParentescoCodigo,
            ["sexoCodigo"] = fam.SexoCodigo,
            ["nombre"] = fam.Nombre,
            ["apellido"] = fam.Apellido,
            ["fechaNacimiento"] = fam.FechaNacimiento.Date,
            ["fechaActualizacion"] = DateTime.Today
        };
        Empleados.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", fam.CodigoEmpleado),
            Builders<BsonDocument>.Update.Push("familiares", item));
        ActualizarConteoFamiliares(fam.CodigoEmpleado);
    }

    public void EliminarFamiliar(string codigoFamiliar)
    {
        var doc = Empleados.Find(Builders<BsonDocument>.Filter.Eq("familiares.codigo", codigoFamiliar)).FirstOrDefault();
        string? empCodigo = doc?["_id"].AsString;
        if (empCodigo == null) return;

        Empleados.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", empCodigo),
            Builders<BsonDocument>.Update.PullFilter("familiares",
                Builders<BsonDocument>.Filter.Eq("codigo", codigoFamiliar)));
        ActualizarConteoFamiliares(empCodigo);
    }

    private void ActualizarConteoFamiliares(string codigoEmpleado)
    {
        var doc = Empleados.Find(Builders<BsonDocument>.Filter.Eq("_id", codigoEmpleado)).FirstOrDefault();
        int count = doc != null && doc.Contains("familiares") ? doc["familiares"].AsBsonArray.Count : 0;
        Empleados.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", codigoEmpleado),
            Builders<BsonDocument>.Update.Set("cargosFamiliares", count));
    }

    // ==================== CATÁLOGOS ====================

    public Dictionary<string, string> ListarSexos() => new(Sexos);

    public Dictionary<string, string> ListarEstadosCiviles()
    {
        var dict = new Dictionary<string, string>();
        foreach (var d in EstadosCiviles.Find(FilterDefinition<BsonDocument>.Empty).ToList())
            dict[d["_id"].AsString] = d["descripcion"].AsString;
        return dict;
    }

    public Dictionary<string, string> ListarDepartamentos()
    {
        var dict = new Dictionary<string, string>();
        foreach (var d in Departamentos.Find(FilterDefinition<BsonDocument>.Empty)
                     .SortBy(x => x["descripcion"]).ToList())
            dict[d["_id"].AsString] = d["descripcion"].AsString;
        return dict;
    }

    public List<Cargo> ListarCargos()
    {
        var lista = new List<Cargo>();
        foreach (var dep in Departamentos.Find(FilterDefinition<BsonDocument>.Empty).ToList())
        {
            string depCod = dep["_id"].AsString;
            string depDesc = dep["descripcion"].AsString;
            if (!dep.Contains("cargos")) continue;
            foreach (var c in dep["cargos"].AsBsonArray.Select(x => x.AsBsonDocument))
            {
                lista.Add(new Cargo
                {
                    DepartamentoCodigo = depCod,
                    DepartamentoDescripcion = depDesc,
                    Codigo = c["codigo"].AsString,
                    Descripcion = c["descripcion"].AsString
                });
            }
        }
        return [.. lista.OrderBy(c => c.DepartamentoDescripcion).ThenBy(c => c.Descripcion)];
    }

    public Dictionary<string, string> ListarParentescos()
    {
        var dict = new Dictionary<string, string>();
        foreach (var p in Parentescos.Find(FilterDefinition<BsonDocument>.Empty).SortBy(x => x["_id"]).ToList())
            dict[p["_id"].AsString] = p["descripcion"].AsString;
        if (dict.Count == 0)
        {
            dict["PRT001"] = "Padre"; dict["PRT002"] = "Madre";
            dict["PRT003"] = "Cónyuge"; dict["PRT004"] = "Hijo/a";
        }
        return dict;
    }

    public List<string> ListarTitulosEducacion()
    {
        var base_ = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Ingeniería en Software", "Ingeniería en Sistemas",
            "Ingeniería en Tecnologías de la Información", "Ingeniería Industrial",
            "Licenciatura en Administración de Empresas", "Tecnólogo Superior",
            "Tecnólogo Superior en Desarrollo de Software",
            "Bachiller General Unificado", "Licenciatura en Contabilidad y Auditoría"
        };
        foreach (var doc in Empleados.Find(FilterDefinition<BsonDocument>.Empty).ToList())
        {
            if (!doc.Contains("educacion")) continue;
            foreach (var e in doc["educacion"].AsBsonArray.Select(x => x.AsBsonDocument))
            {
                var titulo = e.GetValue("titulo", "").AsString;
                if (!string.IsNullOrWhiteSpace(titulo)) base_.Add(titulo);
            }
        }
        return [.. base_.OrderBy(x => x)];
    }

    public List<string> ListarInstitucionesEducacion()
    {
        var base_ = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Escuela Politécnica Nacional (EPN)", "Universidad Central del Ecuador (UCE)",
            "Pontificia Universidad Católica del Ecuador (PUCE)",
            "Universidad de las Fuerzas Armadas (ESPE)",
            "Universidad San Francisco de Quito (USFQ)",
            "Escuela Superior Politécnica del Litoral (ESPOL)",
            "Universidad de Cuenca", "Universidad Técnica Particular de Loja (UTPL)"
        };
        foreach (var doc in Empleados.Find(FilterDefinition<BsonDocument>.Empty).ToList())
        {
            if (!doc.Contains("educacion")) continue;
            foreach (var e in doc["educacion"].AsBsonArray.Select(x => x.AsBsonDocument))
            {
                var inst = e.GetValue("institucion", "").AsString;
                if (!string.IsNullOrWhiteSpace(inst)) base_.Add(inst);
            }
        }
        return [.. base_.OrderBy(x => x)];
    }

    public bool EsCargoValidoParaDepartamento(string depCodigo, string carCodigo)
    {
        var dep = Departamentos.Find(Builders<BsonDocument>.Filter.Eq("_id", depCodigo)).FirstOrDefault();
        if (dep == null || !dep.Contains("cargos")) return false;
        return dep["cargos"].AsBsonArray.Any(c => c["codigo"].AsString == carCodigo);
    }

    public Cargo? ObtenerCargoPorCodigo(string cargoCodigo)
    {
        foreach (var dep in Departamentos.Find(FilterDefinition<BsonDocument>.Empty).ToList())
        {
            if (!dep.Contains("cargos")) continue;
            foreach (var c in dep["cargos"].AsBsonArray.Select(x => x.AsBsonDocument))
            {
                if (c["codigo"].AsString == cargoCodigo)
                {
                    return new Cargo
                    {
                        DepartamentoCodigo = dep["_id"].AsString,
                        DepartamentoDescripcion = dep["descripcion"].AsString,
                        Codigo = c["codigo"].AsString,
                        Descripcion = c["descripcion"].AsString
                    };
                }
            }
        }
        return null;
    }

    // ==================== HELPERS ====================

    private static Empleado Mapear(BsonDocument doc, Dictionary<string, BsonDocument> dictDep)
    {
        string depCod = LeerId(doc, "departamentoCodigo");
        string carCod = LeerId(doc, "cargoCodigo");
        string? depDesc = null, carDesc = null;
        if (dictDep.TryGetValue(depCod, out var dep))
        {
            depDesc = dep.GetValue("descripcion", "").AsString;
            if (dep.Contains("cargos"))
            {
                var cargo = dep["cargos"].AsBsonArray
                    .Select(x => x.AsBsonDocument)
                    .FirstOrDefault(c => c["codigo"].AsString == carCod);
                carDesc = cargo?["descripcion"].AsString;
            }
        }
        string sexoCod = LeerId(doc, "sexoCodigo");
        string estCivCod = LeerId(doc, "estadoCivilCodigo");
        Sexos.TryGetValue(sexoCod, out var sexoDesc);

        return new Empleado
        {
            Codigo = doc["_id"].AsString,
            Nombre = doc["nombre"].AsString,
            Apellido = doc["apellido"].AsString,
            Cedula = doc["cedula"].AsString,
            Email = doc["email"].AsString,
            Telefono = doc["telefono"].AsString,
            DepartamentoCodigo = depCod,
            CargoCodigo = carCod,
            SexoCodigo = sexoCod,
            EstadoCivilCodigo = string.IsNullOrEmpty(estCivCod) ? null : estCivCod,
            FechaNacimiento = doc["fechaNacimiento"].ToUniversalTime(),
            FechaSalida = doc["fechaSalida"].ToUniversalTime(),
            Direccion = doc["direccion"].AsString,
            Pasaporte = doc.GetValue("pasaporte", "N/A").AsString,
            CargosFamiliares = doc.GetValue("cargosFamiliares", 0).ToInt32(),
            FotoRuta = LeerStr(doc, "fotoRuta"),
            DepartamentoDescripcion = depDesc,
            CargoDescripcion = carDesc,
            SexoDescripcion = sexoDesc,
            EstadoCivilDescripcion = null
        };
    }

    private static Educacion MapearEducacion(BsonDocument doc, string codigoEmpleado) => new()
    {
        Codigo = doc["codigo"].AsString,
        CodigoEmpleado = codigoEmpleado,
        Titulo = doc["titulo"].AsString,
        Institucion = doc["institucion"].AsString,
        FechaGrado = doc["fechaGrado"].ToUniversalTime(),
        FechaInicio = doc["fechaInicio"].ToUniversalTime()
    };

    private static string LeerId(BsonDocument doc, string campo) =>
        doc.Contains(campo) && !doc[campo].IsBsonNull ? doc[campo].AsString.Trim() : "";

    private static string? LeerStr(BsonDocument doc, string campo) =>
        doc.Contains(campo) && !doc[campo].IsBsonNull ? doc[campo].AsString : null;

    private static BsonValue Nullable(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? BsonNull.Value : valor;
}
