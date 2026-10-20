using GestionProyectosMonster.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestionProyectosMonster.Services;

public class DAOProyecto
{
    private readonly IMongoDatabase _db;
    private IMongoCollection<BsonDocument> Proyectos => _db.GetCollection<BsonDocument>("proyectos");
    private IMongoCollection<BsonDocument> Departamentos => _db.GetCollection<BsonDocument>("departamentos");
    private IMongoCollection<BsonDocument> Counters => _db.GetCollection<BsonDocument>("counters");

    public DAOProyecto(IMongoDatabase db) => _db = db;

    public List<Proyecto> Listar()
    {
        var dictDep = Departamentos.Find(FilterDefinition<BsonDocument>.Empty)
            .ToList().ToDictionary(d => d["_id"].AsString, d => d["descripcion"].AsString);

        return Proyectos.Find(FilterDefinition<BsonDocument>.Empty)
            .SortBy(p => p["_id"])
            .ToList()
            .Select(p => Mapear(p, dictDep))
            .ToList();
    }

    public List<Proyecto> ListarPorEmpleado(string codigoEmpleado)
    {
        var dictDep = Departamentos.Find(FilterDefinition<BsonDocument>.Empty)
            .ToList().ToDictionary(d => d["_id"].AsString, d => d["descripcion"].AsString);

        var filtro = Builders<BsonDocument>.Filter.ElemMatch("empleadosAsignados",
            Builders<BsonDocument>.Filter.Eq("empleadoCodigo", codigoEmpleado));

        return Proyectos.Find(filtro)
            .SortBy(p => p["_id"])
            .ToList()
            .Select(p => Mapear(p, dictDep))
            .ToList();
    }

    public string GenerarCodigoSiguiente()
    {
        var filtro = Builders<BsonDocument>.Filter.Eq("_id", "proyecto");
        var update = Builders<BsonDocument>.Update.Inc("seq", 1);
        var opciones = new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
        var doc = Counters.FindOneAndUpdate(filtro, update, opciones);
        int seq = doc["seq"].ToInt32();
        return $"P{seq:D2}";
    }

    public bool Insertar(Proyecto p)
    {
        var doc = new BsonDocument
        {
            ["_id"] = p.Codigo.Trim().ToUpper(),
            ["nombre"] = p.Nombre.Trim(),
            ["departamentoCodigo"] = string.IsNullOrWhiteSpace(p.DepartamentoCodigo) ? BsonNull.Value : p.DepartamentoCodigo,
            ["empleadosAsignados"] = new BsonArray()
        };
        try { Proyectos.InsertOne(doc); return true; }
        catch (MongoWriteException) { return false; }
    }

    public bool Actualizar(Proyecto p)
    {
        var result = Proyectos.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", p.Codigo.Trim().ToUpper()),
            Builders<BsonDocument>.Update
                .Set("nombre", p.Nombre.Trim())
                .Set("departamentoCodigo", string.IsNullOrWhiteSpace(p.DepartamentoCodigo) ? BsonNull.Value : (BsonValue)p.DepartamentoCodigo));
        return result.ModifiedCount > 0;
    }

    public bool Eliminar(string codigo)
    {
        var result = Proyectos.DeleteOne(Builders<BsonDocument>.Filter.Eq("_id", codigo));
        return result.DeletedCount > 0;
    }

    public List<(string CodigoEmpleado, decimal? HorasEstimadas)> ListarEmpleadosAsignados(string codigoProyecto)
    {
        var p = Proyectos.Find(Builders<BsonDocument>.Filter.Eq("_id", codigoProyecto)).FirstOrDefault();
        if (p == null || !p.Contains("empleadosAsignados")) return [];
        return p["empleadosAsignados"].AsBsonArray
            .Select(x => x.AsBsonDocument)
            .Select(e => (e["empleadoCodigo"].AsString.Trim(),
                          e["horasEstimadas"].IsBsonNull ? (decimal?)null : (decimal)e["horasEstimadas"].ToDecimal()))
            .ToList();
    }

    public void AsignarEmpleados(string codigoProyecto, IEnumerable<(string Codigo, decimal? Horas)> empleados)
    {
        var arr = new BsonArray();
        foreach (var (codigo, horas) in empleados)
        {
            arr.Add(new BsonDocument
            {
                ["empleadoCodigo"] = codigo.Trim(),
                ["horasEstimadas"] = horas.HasValue ? new BsonDecimal128(horas.Value) : BsonNull.Value
            });
        }
        Proyectos.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", codigoProyecto),
            Builders<BsonDocument>.Update.Set("empleadosAsignados", arr));
    }

    private static Proyecto Mapear(BsonDocument p, Dictionary<string, string> dictDep)
    {
        string? depCod = p.Contains("departamentoCodigo") && !p["departamentoCodigo"].IsBsonNull ? p["departamentoCodigo"].AsString : null;
        return new Proyecto
        {
            Codigo = p["_id"].AsString,
            Nombre = p["nombre"].AsString,
            DepartamentoCodigo = depCod,
            DepartamentoDescripcion = depCod != null && dictDep.TryGetValue(depCod, out var d) ? d : null
        };
    }
}
