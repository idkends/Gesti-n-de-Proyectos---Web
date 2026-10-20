using GestionProyectosMonster.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GestionProyectosMonster.Services;

public class DAOHoras
{
    private readonly IMongoDatabase _db;
    private IMongoCollection<BsonDocument> RegistroHoras => _db.GetCollection<BsonDocument>("registroHoras");
    private IMongoCollection<BsonDocument> Proyectos => _db.GetCollection<BsonDocument>("proyectos");
    private IMongoCollection<BsonDocument> Empleados => _db.GetCollection<BsonDocument>("empleados");
    private IMongoCollection<BsonDocument> Counters => _db.GetCollection<BsonDocument>("counters");

    public DAOHoras(IMongoDatabase db) => _db = db;

    // ── Generar código siguiente ──────────────────────────────────
    public string GenerarCodigoSiguiente()
    {
        var filtro = Builders<BsonDocument>.Filter.Eq("_id", "registroHoras");
        var update = Builders<BsonDocument>.Update.Inc("seq", 1);
        var opciones = new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
        var doc = Counters.FindOneAndUpdate(filtro, update, opciones);
        int seq = doc["seq"].ToInt32();
        return $"TI{seq:D4}";
    }

    // ── Proyectos asignados al empleado con horas esta semana ─────
    public List<RegistroHoras> ListarSemanaEmpleado(string codigoEmpleado, DateTime lunes)
    {
        var lista = new List<RegistroHoras>();
        var proyectos = Proyectos.Find(Builders<BsonDocument>.Filter.ElemMatch("empleadosAsignados",
            Builders<BsonDocument>.Filter.Eq("empleadoCodigo", codigoEmpleado)))
            .SortBy(p => p["_id"]).ToList();

        foreach (var p in proyectos)
        {
            string codigoProyecto = p["_id"].AsString;
            var asignacion = p["empleadosAsignados"].AsBsonArray
                .Select(x => x.AsBsonDocument)
                .FirstOrDefault(e => e["empleadoCodigo"].AsString == codigoEmpleado);
            decimal? horasEstimadas = asignacion != null && !asignacion["horasEstimadas"].IsBsonNull
                ? asignacion["horasEstimadas"].ToDecimal() : null;

            var t = RegistroHoras.Find(Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("empleadoCodigo", codigoEmpleado),
                Builders<BsonDocument>.Filter.Eq("proyectoCodigo", codigoProyecto),
                Builders<BsonDocument>.Filter.Eq("fechaSemana", lunes.Date))).FirstOrDefault();

            lista.Add(new RegistroHoras
            {
                CodigoEmpleado = codigoEmpleado,
                CodigoProyecto = codigoProyecto,
                NombreProyecto = p["nombre"].AsString,
                HorasEstimadas = horasEstimadas,
                Codigo = t != null ? t["_id"].AsString : "",
                Horas = t != null ? t["horas"].ToDecimal() : 0m,
                Estado = t != null ? t["estado"].AsString : "P",
                Observaciones = t != null && t.Contains("observaciones") && !t["observaciones"].IsBsonNull ? t["observaciones"].AsString : null,
                FechaCreacion = t != null && t.Contains("fechaCreacion") ? t["fechaCreacion"].ToUniversalTime() : DateTime.Now,
                FechaModificacion = t != null && t.Contains("fechaModificacion") ? t["fechaModificacion"].ToUniversalTime() : DateTime.Now,
                FechaSemana = lunes.Date
            });
        }
        return lista;
    }

    // ── Guardar (upsert) horas de una semana ─────────────────────
    public void GuardarSemana(string codigoEmpleado, DateTime lunes, Dictionary<string, decimal> horasPorProyecto)
    {
        foreach (var (proyecto, horas) in horasPorProyecto)
        {
            if (horas < 0) continue;

            var existente = RegistroHoras.Find(Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("empleadoCodigo", codigoEmpleado),
                Builders<BsonDocument>.Filter.Eq("proyectoCodigo", proyecto),
                Builders<BsonDocument>.Filter.Eq("fechaSemana", lunes.Date),
                Builders<BsonDocument>.Filter.Eq("estado", "P"))).FirstOrDefault();

            if (existente != null)
            {
                if (horas == 0)
                {
                    RegistroHoras.DeleteOne(Builders<BsonDocument>.Filter.Eq("_id", existente["_id"]));
                }
                else
                {
                    RegistroHoras.UpdateOne(
                        Builders<BsonDocument>.Filter.Eq("_id", existente["_id"]),
                        Builders<BsonDocument>.Update.Set("horas", horas).Set("fechaModificacion", DateTime.Now));
                }
            }
            else if (horas > 0)
            {
                string nuevo = GenerarCodigoSiguiente();
                var doc = new BsonDocument
                {
                    ["_id"] = nuevo,
                    ["empleadoCodigo"] = codigoEmpleado,
                    ["proyectoCodigo"] = proyecto,
                    ["fechaSemana"] = lunes.Date,
                    ["horas"] = horas,
                    ["estado"] = "P",
                    ["observaciones"] = BsonNull.Value,
                    ["fechaCreacion"] = DateTime.Now,
                    ["fechaModificacion"] = DateTime.Now
                };
                RegistroHoras.InsertOne(doc);
            }
        }
    }

    // ── Listar pendientes por departamento (Jefe) ─────────────────
    public List<RegistroHoras> ListarPendientesPorDepartamento(string departamentoCodigo)
    {
        var codigosProyecto = Proyectos.Find(Builders<BsonDocument>.Filter.Eq("departamentoCodigo", departamentoCodigo))
            .ToList().Select(p => p["_id"].AsString).ToList();
        var filtro = Builders<BsonDocument>.Filter.In("proyectoCodigo", codigosProyecto);
        return EjecutarListado(filtro);
    }

    // ── Listar todos (Admin) ──────────────────────────────────────
    public List<RegistroHoras> ListarTodos() => EjecutarListado(FilterDefinition<BsonDocument>.Empty);

    // ── Reporte: estimadas vs. reales por proyecto ────────────────
    public List<(string CodigoProyecto, string NombreProyecto, decimal HorasEstimadas, decimal HorasRealesAprobadas, int TotalRegistros)>
        ReportePorProyecto(string? departamentoCodigo = null)
    {
        var filtroProy = departamentoCodigo != null
            ? Builders<BsonDocument>.Filter.Eq("departamentoCodigo", departamentoCodigo)
            : FilterDefinition<BsonDocument>.Empty;

        var proyectos = Proyectos.Find(filtroProy).SortBy(p => p["_id"]).ToList();
        var resultado = new List<(string, string, decimal, decimal, int)>();

        foreach (var p in proyectos)
        {
            string codigo = p["_id"].AsString;
            decimal estimadas = p.Contains("empleadosAsignados")
                ? p["empleadosAsignados"].AsBsonArray.Select(x => x.AsBsonDocument)
                    .Where(e => !e["horasEstimadas"].IsBsonNull)
                    .Sum(e => e["horasEstimadas"].ToDecimal())
                : 0m;

            var registros = RegistroHoras.Find(Builders<BsonDocument>.Filter.Eq("proyectoCodigo", codigo)).ToList();
            decimal reales = registros.Where(t => t["estado"].AsString == "A").Sum(t => t["horas"].ToDecimal());
            int total = registros.Count;

            resultado.Add((codigo, p["nombre"].AsString, estimadas, reales, total));
        }
        return resultado;
    }

    // ── Aprobar / Rechazar ────────────────────────────────────────
    public void CambiarEstado(string codigoRegistro, string estado, string? observaciones)
    {
        RegistroHoras.UpdateOne(
            Builders<BsonDocument>.Filter.Eq("_id", codigoRegistro),
            Builders<BsonDocument>.Update
                .Set("estado", estado)
                .Set("observaciones", string.IsNullOrWhiteSpace(observaciones) ? BsonNull.Value : (BsonValue)observaciones)
                .Set("fechaModificacion", DateTime.Now));
    }

    // ── Privados ─────────────────────────────────────────────────
    private List<RegistroHoras> EjecutarListado(FilterDefinition<BsonDocument> filtro)
    {
        var dictEmp = Empleados.Find(FilterDefinition<BsonDocument>.Empty).ToList()
            .ToDictionary(e => e["_id"].AsString, e => $"{e["nombre"].AsString} {e["apellido"].AsString}");
        var dictProy = Proyectos.Find(FilterDefinition<BsonDocument>.Empty).ToList()
            .ToDictionary(p => p["_id"].AsString, p => p);

        var lista = RegistroHoras.Find(filtro).ToList()
            .Select(t =>
            {
                string codProy = t["proyectoCodigo"].AsString;
                decimal? horasEst = null;
                if (dictProy.TryGetValue(codProy, out var proy) && proy.Contains("empleadosAsignados"))
                {
                    var asign = proy["empleadosAsignados"].AsBsonArray.Select(x => x.AsBsonDocument)
                        .FirstOrDefault(e => e["empleadoCodigo"].AsString == t["empleadoCodigo"].AsString);
                    if (asign != null && !asign["horasEstimadas"].IsBsonNull) horasEst = asign["horasEstimadas"].ToDecimal();
                }
                return new RegistroHoras
                {
                    Codigo = t["_id"].AsString,
                    CodigoEmpleado = t["empleadoCodigo"].AsString,
                    NombreEmpleado = dictEmp.TryGetValue(t["empleadoCodigo"].AsString, out var n) ? n : "",
                    CodigoProyecto = codProy,
                    NombreProyecto = dictProy.TryGetValue(codProy, out var p) ? p["nombre"].AsString : "",
                    FechaSemana = t["fechaSemana"].ToUniversalTime(),
                    Horas = t["horas"].ToDecimal(),
                    HorasEstimadas = horasEst,
                    Estado = t["estado"].AsString,
                    Observaciones = t.Contains("observaciones") && !t["observaciones"].IsBsonNull ? t["observaciones"].AsString : null,
                    FechaCreacion = t["fechaCreacion"].ToUniversalTime(),
                    FechaModificacion = t["fechaModificacion"].ToUniversalTime()
                };
            })
            .OrderBy(r => r.Estado)
            .ThenByDescending(r => r.FechaSemana)
            .ToList();
        return lista;
    }
}
