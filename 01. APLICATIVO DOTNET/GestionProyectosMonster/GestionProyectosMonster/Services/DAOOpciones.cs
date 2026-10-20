using GestionProyectosMonster.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestionProyectosMonster.Services;

public class DAOOpciones
{
    private readonly IMongoDatabase _db;
    private IMongoCollection<BsonDocument> Opciones => _db.GetCollection<BsonDocument>("opciones");
    private IMongoCollection<BsonDocument> Perfiles => _db.GetCollection<BsonDocument>("perfiles");

    public DAOOpciones(IMongoDatabase db) => _db = db;

    public List<Opcion> Listar()
    {
        return Opciones.Find(FilterDefinition<BsonDocument>.Empty)
            .SortBy(o => o["sistemaCodigo"]).ThenBy(o => o["_id"])
            .ToList()
            .Select(Mapear)
            .ToList();
    }

    public HashSet<string> GetCodigosParaPerfil(string perfilCodigo)
    {
        var set = new HashSet<string>();
        var perfil = Perfiles.Find(Builders<BsonDocument>.Filter.Eq("_id", perfilCodigo.Trim())).FirstOrDefault();
        if (perfil == null || !perfil.Contains("opcionesAsignadas")) return set;
        foreach (var o in perfil["opcionesAsignadas"].AsBsonArray.Select(x => x.AsBsonDocument))
        {
            if (!o.Contains("fechaRetiro") || o["fechaRetiro"].IsBsonNull)
                set.Add(o["opcionCodigo"].AsString);
        }
        return set;
    }

    public bool AsignarOpciones(string perfilCodigo, List<string> codigos)
    {
        string perfil = perfilCodigo.Trim();
        if (string.IsNullOrEmpty(perfil)) return false;

        var arr = new BsonArray();
        foreach (var codigo in codigos.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            arr.Add(new BsonDocument
            {
                ["opcionCodigo"] = codigo.Trim(),
                ["fechaAsignacion"] = DateTime.Today,
                ["fechaRetiro"] = BsonNull.Value
            });
        }

        Perfiles.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", perfil),
            Builders<BsonDocument>.Update.Set("opcionesAsignadas", arr));
        return true;
    }

    private static Opcion Mapear(BsonDocument o)
    {
        var codigo = o["_id"].AsString.Trim();
        return new Opcion
        {
            Codigo             = codigo,
            SistemaCodigo      = o["sistemaCodigo"].AsString,
            SistemaDescripcion = o.Contains("sistemaDescripcion") && !o["sistemaDescripcion"].IsBsonNull ? o["sistemaDescripcion"].AsString : "",
            Descripcion        = o["descripcion"].AsString,
            SubGrupo           = ResolverSubGrupo(codigo)
        };
    }

    private static string ResolverSubGrupo(string codigo) => codigo switch
    {
        "003" or "008" or "002" or "017" or "012" => "Tablas Básicas",
        "009" or "010" or "013" or "014" or "016"
            or "004" or "005" or "006" or "007"   => "Procesos",
        "011" or "015"                             => "Reportes",
        _                                          => "General"
    };
}
