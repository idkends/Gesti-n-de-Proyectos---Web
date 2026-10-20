using GestionProyectosMonster.Services;
using GestionProyectosMonster.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectosMonster.Controllers;

[Authorize(Roles = $"{Usuario.ROL_ADMINISTRADOR},{Usuario.ROL_JEFE_DEPARTAMENTO}")]
public class ProyectoController : Controller
{
    private readonly DAOProyecto _dao;
    private readonly DAOEmpleado _daoEmp;
    private readonly DAODepartamento _daoDep;

    public ProyectoController(DAOProyecto dao, DAOEmpleado daoEmp, DAODepartamento daoDep)
    {
        _dao = dao;
        _daoEmp = daoEmp;
        _daoDep = daoDep;
    }

    public IActionResult Index()
    {
        ViewBag.Departamentos = _daoDep.Listar();
        return View(_dao.Listar());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Guardar(Proyecto proyecto)
    {
        try
        {
            bool existe = _dao.Listar().Any(p => p.Codigo == proyecto.Codigo.Trim().ToUpper());
            if (existe)
                _dao.Actualizar(proyecto);
            else
                _dao.Insertar(proyecto);

            TempData["Exito"] = existe ? "Proyecto actualizado." : "Proyecto creado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(string codigo)
    {
        try
        {
            _dao.Eliminar(codigo);
            TempData["Exito"] = "Proyecto eliminado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }

    public IActionResult AsignarPersonal()
    {
        return View(_dao.Listar());
    }

    [HttpGet]
    public IActionResult Asignar(string codigo)
    {
        var proyectos = _dao.Listar();
        var proyecto = proyectos.FirstOrDefault(p => p.Codigo == codigo);
        if (proyecto == null) return NotFound();

        ViewBag.Proyecto = proyecto;
        ViewBag.Empleados = _daoEmp.Listar();
        ViewBag.Asignados = _dao.ListarEmpleadosAsignados(codigo);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Asignar(string codigo, List<string>? empleados, List<decimal?>? horasEstimadas)
    {
        try
        {
            var lista = (empleados ?? [])
                .Select((e, i) => (e, horasEstimadas != null && i < horasEstimadas.Count ? horasEstimadas[i] : null))
                .ToList();
            _dao.AsignarEmpleados(codigo, lista);
            TempData["Exito"] = "Asignación actualizada.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }
}
