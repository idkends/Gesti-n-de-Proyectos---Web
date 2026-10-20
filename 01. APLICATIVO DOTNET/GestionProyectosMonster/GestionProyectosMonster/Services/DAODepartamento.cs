using GestionProyectosMonster.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestionProyectosMonster.Services;

public class DAODepartamento
{
    private readonly IMongoDatabase _db;
    private IMongoCollection<BsonDocument> Departamentos => _db.GetCollection<BsonDocument>("departamentos");
    private IMongoCollection<BsonDocument> Empleados => _db.GetCollection<BsonDocument>("empleados");
    private IMongoCollection<BsonDocument> Proyectos => _db.GetCollection<BsonDocument>("proyectos");

    public DAODepartamento(IMongoDatabase db) => _db = db;

    public List<Departamento> Listar()
    {
        return Departamentos.Find(FilterDefinition<BsonDocument>.Empty)
            .SortBy(d => d["_id"])
            .ToList()
            .Select(d => new Departamento { Codigo = d["_id"].AsString, Descripcion = d["descripcion"].AsString })
            .ToList();
    }

    public Departamento? BuscarPorCodigo(string codigo)
    {
        var d = Departamentos.Find(Builders<BsonDocument>.Filter.Eq("_id", codigo.Trim().ToUpper())).FirstOrDefault();
        return d == null ? null : new Departamento { Codigo = d["_id"].AsString, Descripcion = d["descripcion"].AsString };
    }

    public bool Insertar(Departamento dep)
    {
        var doc = new BsonDocument
        {
            ["_id"] = dep.Codigo.Trim().ToUpper(),
            ["descripcion"] = dep.Descripcion.Trim(),
            ["cargos"] = new BsonArray()
        };
        try { Departamentos.InsertOne(doc); return true; }
        catch (MongoWriteException) { return false; }
    }

    public bool Actualizar(Departamento dep)
    {
        var result = Departamentos.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", dep.Codigo.Trim().ToUpper()),
            Builders<BsonDocument>.Update.Set("descripcion", dep.Descripcion.Trim()));
        return result.ModifiedCount > 0;
    }

    public bool Eliminar(string codigo)
    {
        var result = Departamentos.DeleteOne(Builders<BsonDocument>.Filter.Eq("_id", codigo.Trim().ToUpper()));
        return result.DeletedCount > 0;
    }

    public string? MotivoNoPuedeEliminar(string codigo)
    {
        string c = codigo.Trim().ToUpper();
        var motivos = new List<string>();

        long emp = Empleados.CountDocuments(Builders<BsonDocument>.Filter.Eq("departamentoCodigo", c));
        if (emp > 0) motivos.Add(emp == 1 ? "1 empleado asociado" : $"{emp} empleados asociados");

        var dep = Departamentos.Find(Builders<BsonDocument>.Filter.Eq("_id", c)).FirstOrDefault();
        int cargos = dep != null && dep.Contains("cargos") ? dep["cargos"].AsBsonArray.Count : 0;
        if (cargos > 0) motivos.Add(cargos == 1 ? "1 cargo asociado" : $"{cargos} cargos asociados");

        long proyectos = Proyectos.CountDocuments(Builders<BsonDocument>.Filter.Eq("departamentoCodigo", c));
        if (proyectos > 0) motivos.Add(proyectos == 1 ? "1 proyecto asociado" : $"{proyectos} proyectos asociados");

        return motivos.Count == 0 ? null
            : $"No se puede eliminar el departamento porque tiene {string.Join(", ", motivos)}.";
    }
}
