using GestionProyectosMonster.Services;
using GestionProyectosMonster.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestionProyectosMonster.Controllers;

[Authorize]
public class HorasController : Controller
{
    private readonly DAOHoras    _dao;
    private readonly DAOProyecto _daoPro;

    public HorasController(DAOHoras dao, DAOProyecto daoPro)
    {
        _dao    = dao;
        _daoPro = daoPro;
    }

    // ==================== EMPLEADO: Registrar horas ====================

    [Authorize(Roles = $"{Usuario.ROL_EMPLEADO},{Usuario.ROL_ADMINISTRADOR}")]
    [HttpGet]
    public IActionResult Registrar(string? semana)
    {
        string empCodigo = User.FindFirstValue(ClaimTypes.Name) ?? "";
        DateTime lunes   = ObtenerLunes(semana);

        ViewBag.Semana = lunes.ToString("yyyy-MM-dd");
        ViewBag.SemanaTxt = $"Semana del {lunes:dd/MM/yyyy} al {lunes.AddDays(4):dd/MM/yyyy}";
        return View(_dao.ListarSemanaEmpleado(empCodigo, lunes));
    }

    [Authorize(Roles = $"{Usuario.ROL_EMPLEADO},{Usuario.ROL_ADMINISTRADOR}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Registrar(string semana, List<string> proyectos, List<decimal> horas)
    {
        string empCodigo = User.FindFirstValue(ClaimTypes.Name) ?? "";
        DateTime lunes   = ObtenerLunes(semana);

        try
        {
            var dict = proyectos
                .Zip(horas, (p, h) => (p, h))
                .ToDictionary(x => x.p, x => x.h);
            _dao.GuardarSemana(empCodigo, lunes, dict);
            TempData["Exito"] = "Horas guardadas. Quedan pendientes de aprobación.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }
        return RedirectToAction(nameof(Registrar), new { semana });
    }

    // ==================== JEFE: Aprobar / Rechazar ====================

    [Authorize(Roles = $"{Usuario.ROL_JEFE_DEPARTAMENTO},{Usuario.ROL_ADMINISTRADOR}")]
    [HttpGet]
    public IActionResult Aprobar()
    {
        string empCodigo = User.FindFirstValue(ClaimTypes.Name) ?? "";
        string rol       = User.FindFirstValue(ClaimTypes.Role) ?? "";

        List<RegistroHoras> registros;
        if (rol == Usuario.ROL_ADMINISTRADOR)
            registros = _dao.ListarTodos();
        else
        {
            var proyectos = _daoPro.ListarPorEmpleado(empCodigo);
            string? deptCodigo = proyectos.FirstOrDefault()?.DepartamentoCodigo;
            registros = deptCodigo != null
                ? _dao.ListarPendientesPorDepartamento(deptCodigo)
                : new List<RegistroHoras>();
        }
        return View(registros);
    }

    [Authorize(Roles = $"{Usuario.ROL_JEFE_DEPARTAMENTO},{Usuario.ROL_ADMINISTRADOR}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CambiarEstado(string codigoRegistro, string estado, string? observaciones)
    {
        try
        {
            _dao.CambiarEstado(codigoRegistro, estado, observaciones);
            TempData["Exito"] = estado == "A" ? "Horas aprobadas." : "Horas rechazadas.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }
        return RedirectToAction(nameof(Aprobar));
    }

    // ==================== JEFE/ADMIN: Reporte comparativo ====================

    [Authorize(Roles = $"{Usuario.ROL_JEFE_DEPARTAMENTO},{Usuario.ROL_ADMINISTRADOR}")]
    [HttpGet]
    public IActionResult Reporte()
    {
        string empCodigo = User.FindFirstValue(ClaimTypes.Name) ?? "";
        string rol       = User.FindFirstValue(ClaimTypes.Role) ?? "";

        string? deptCodigo = null;
        if (rol == Usuario.ROL_JEFE_DEPARTAMENTO)
        {
            var proyectos = _daoPro.ListarPorEmpleado(empCodigo);
            deptCodigo = proyectos.FirstOrDefault()?.DepartamentoCodigo;
        }

        ViewBag.GeneradoPor = User.FindFirstValue(ClaimTypes.GivenName) ?? "Sistema";
        ViewBag.FechaGen    = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        return View(_dao.ReportePorProyecto(deptCodigo));
    }

    // ── Utilitario ─────────────────────────────────────────────────
    private static DateTime ObtenerLunes(string? semanaStr)
    {
        if (DateTime.TryParse(semanaStr, out DateTime parsed))
        {
            int diff = ((int)parsed.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return parsed.Date.AddDays(-diff);
        }
        DateTime hoy = DateTime.Today;
        int dias = ((int)hoy.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return hoy.AddDays(-dias);
    }
}
