using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace GestionProyectosMonster.Services;

public interface IOpcionesService
{
    HashSet<string> GetOpciones(string codigoEmpleado);
    bool TieneOpcion(string codigoEmpleado, string codigoOpcion);
    bool TieneOpcionCualquiera(string codigoEmpleado, params string[] codigos);
    void InvalidarUsuario(string codigoEmpleado);
    void InvalidarTodos();
}

public class OpcionesService : IOpcionesService
{
    // ── Constantes de opción (espejo de Java DAOOPCIONES) ──────────────────
    public const string OPC_INICIO             = "001";
    public const string OPC_GESTION_USUARIOS   = "002";
    public const string OPC_DEPARTAMENTOS      = "003";
    public const string OPC_ASIGNAR_PERFILES   = "004";
    public const string OPC_GESTION_CLAVE      = "005";
    public const string OPC_ASIGNAR_OPCIONES   = "006";
    public const string OPC_CAMBIAR_CLAVE      = "007";
    public const string OPC_GESTION_PERSONAL   = "008";
    public const string OPC_FAMILIARES         = "009";
    public const string OPC_ORGANIGRAMA        = "010";
    public const string OPC_REPORTE_PERSONAL   = "011";
    public const string OPC_PROYECTOS          = "012";
    public const string OPC_ASIGNAR_PERSONAL   = "013";
    public const string OPC_APROBACION_HORAS   = "014";
    public const string OPC_REPORTES_HORAS     = "015";
    public const string OPC_REGISTRAR_HORAS    = "016";
    public const string OPC_GESTION_PERFILES   = "017";

    private readonly IMongoDatabase _db;
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _clavesCacheadas =
        new(StringComparer.OrdinalIgnoreCase);

    private IMongoCollection<BsonDocument> Usuarios => _db.GetCollection<BsonDocument>("usuarios");
    private IMongoCollection<BsonDocument> Perfiles => _db.GetCollection<BsonDocument>("perfiles");

    public OpcionesService(IMongoDatabase db, IMemoryCache cache)
    {
        _db    = db;
        _cache = cache;
    }

    // ── API pública ───────────────────────────────────────────────────────

    public HashSet<string> GetOpciones(string codigoEmpleado)
    {
        if (string.IsNullOrWhiteSpace(codigoEmpleado)) return [];
        string key = CacheKey(codigoEmpleado);
        _clavesCacheadas.TryAdd(key, 0);
        return _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            return CargarDesdeBD(codigoEmpleado);
        }) ?? [];
    }

    public bool TieneOpcion(string codigoEmpleado, string codigoOpcion) =>
        GetOpciones(codigoEmpleado).Contains(codigoOpcion);

    public bool TieneOpcionCualquiera(string codigoEmpleado, params string[] codigos) =>
        codigos.Any(c => GetOpciones(codigoEmpleado).Contains(c));

    public void InvalidarUsuario(string codigoEmpleado)
    {
        string key = CacheKey(codigoEmpleado);
        _cache.Remove(key);
        _clavesCacheadas.TryRemove(key, out _);
    }

    public void InvalidarTodos()
    {
        foreach (var key in _clavesCacheadas.Keys)
            _cache.Remove(key);
        _clavesCacheadas.Clear();
    }

    // ── Internos ──────────────────────────────────────────────────────────

    private static string CacheKey(string codigoEmpleado) =>
        $"opciones_{codigoEmpleado.Trim().ToUpperInvariant()}";

    private HashSet<string> CargarDesdeBD(string codigoEmpleado)
    {
        var set = new HashSet<string>();
        try
        {
            string codigo = codigoEmpleado.Trim().ToUpperInvariant();
            var usuario = Usuarios.Find(Builders<BsonDocument>.Filter.Eq("_id", codigo)).FirstOrDefault();
            if (usuario == null || !usuario.Contains("perfilesAsignados")) return set;

            var perfilActivo = usuario["perfilesAsignados"].AsBsonArray
                .Select(x => x.AsBsonDocument)
                .Where(p => !p.Contains("fechaRetiro") || p["fechaRetiro"].IsBsonNull)
                .OrderByDescending(p => p["fechaAsignacion"].ToUniversalTime())
                .FirstOrDefault();
            if (perfilActivo == null) return set;

            string perfilCodigo = perfilActivo["perfilCodigo"].AsString;
            var perfil = Perfiles.Find(Builders<BsonDocument>.Filter.Eq("_id", perfilCodigo)).FirstOrDefault();
            if (perfil == null || !perfil.Contains("opcionesAsignadas")) return set;

            foreach (var o in perfil["opcionesAsignadas"].AsBsonArray.Select(x => x.AsBsonDocument))
            {
                if (!o.Contains("fechaRetiro") || o["fechaRetiro"].IsBsonNull)
                    set.Add(o["opcionCodigo"].AsString.Trim());
            }
        }
        catch { /* devuelve set vacío en error de BD */ }
        return set;
    }
}
