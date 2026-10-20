using GestionProyectosMonster.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestionProyectosMonster.Services;

public class DAOPerfil
{
    private readonly IMongoDatabase _db;
    private IMongoCollection<BsonDocument> Perfiles => _db.GetCollection<BsonDocument>("perfiles");
    private IMongoCollection<BsonDocument> Usuarios => _db.GetCollection<BsonDocument>("usuarios");

    public DAOPerfil(IMongoDatabase db) => _db = db;

    public List<Perfil> Listar()
    {
        return Perfiles.Find(FilterDefinition<BsonDocument>.Empty)
            .SortBy(p => p["_id"])
            .ToList()
            .Select(p => new Perfil { Codigo = p["_id"].AsString, Descripcion = p["descripcion"].AsString })
            .ToList();
    }

    public Perfil? BuscarPorCodigo(string codigo)
    {
        var p = Perfiles.Find(Builders<BsonDocument>.Filter.Eq("_id", codigo.Trim())).FirstOrDefault();
        return p == null ? null : new Perfil { Codigo = p["_id"].AsString, Descripcion = p["descripcion"].AsString };
    }

    public string SiguienteCodigo()
    {
        var ultimo = Perfiles.Find(FilterDefinition<BsonDocument>.Empty)
            .SortByDescending(p => p["_id"]).Limit(1).FirstOrDefault();
        if (ultimo != null)
        {
            var digits = new string(ultimo["_id"].AsString.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out int num))
                return $"PERF{num + 1:D4}";
        }
        return "PERF0001";
    }

    public Perfil Insertar(string descripcion)
    {
        string codigo = SiguienteCodigo();
        var doc = new BsonDocument
        {
            ["_id"] = codigo,
            ["descripcion"] = descripcion.Trim(),
            ["opcionesAsignadas"] = new BsonArray()
        };
        Perfiles.InsertOne(doc);
        return new Perfil { Codigo = codigo, Descripcion = descripcion.Trim() };
    }

    public bool Actualizar(string codigo, string descripcion)
    {
        var result = Perfiles.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", codigo.Trim()),
            Builders<BsonDocument>.Update.Set("descripcion", descripcion.Trim()));
        return result.ModifiedCount > 0;
    }

    public bool Eliminar(string codigo)
    {
        string cod = codigo.Trim();
        // Elimina asignaciones activas/históricas de usuarios a este perfil, luego el perfil.
        var usuariosConPerfil = Usuarios.Find(Builders<BsonDocument>.Filter.ElemMatch("perfilesAsignados",
            Builders<BsonDocument>.Filter.Eq("perfilCodigo", cod))).ToList();
        foreach (var u in usuariosConPerfil)
        {
            var arr = u["perfilesAsignados"].AsBsonArray;
            var nuevoArr = new BsonArray(arr.Where(x => x.AsBsonDocument["perfilCodigo"].AsString != cod));
            Usuarios.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", u["_id"]),
                Builders<BsonDocument>.Update.Set("perfilesAsignados", nuevoArr));
        }

        var result = Perfiles.DeleteOne(Builders<BsonDocument>.Filter.Eq("_id", cod));
        return result.DeletedCount > 0;
    }

    public string? MotivoNoSePuedeEliminar(string codigo)
    {
        string cod = codigo.Trim();

        long usuariosActivos = Usuarios.CountDocuments(Builders<BsonDocument>.Filter.ElemMatch("perfilesAsignados",
            Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("perfilCodigo", cod),
                Builders<BsonDocument>.Filter.Eq("fechaRetiro", BsonNull.Value))));
        if (usuariosActivos > 0)
            return $"No se puede eliminar: el perfil tiene {usuariosActivos} usuario{(usuariosActivos == 1 ? "" : "s")} asignado{(usuariosActivos == 1 ? "" : "s")}.";

        var perfil = Perfiles.Find(Builders<BsonDocument>.Filter.Eq("_id", cod)).FirstOrDefault();
        int opciones = perfil != null && perfil.Contains("opcionesAsignadas")
            ? perfil["opcionesAsignadas"].AsBsonArray.Select(x => x.AsBsonDocument)
                .Count(o => !o.Contains("fechaRetiro") || o["fechaRetiro"].IsBsonNull)
            : 0;
        if (opciones > 0)
            return $"No se puede eliminar: el perfil tiene {opciones} opción{(opciones == 1 ? "" : "es")} asignada{(opciones == 1 ? "" : "s")}. Quite las opciones primero.";

        return null;
    }

    public bool ExisteCodigo(string codigo) =>
        Perfiles.Find(Builders<BsonDocument>.Filter.Eq("_id", codigo.Trim())).Any();
}
