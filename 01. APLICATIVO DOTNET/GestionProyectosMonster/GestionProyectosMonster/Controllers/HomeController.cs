using GestionProyectosMonster.Models;
using GestionProyectosMonster.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestionProyectosMonster.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly DAOEmpleado     _daoEmp;
    private readonly DAOProyecto     _daoPro;
    private readonly DAODepartamento _daoDep;

    public HomeController(DAOEmpleado daoEmp, DAOProyecto daoPro, DAODepartamento daoDep)
    {
        _daoEmp = daoEmp;
        _daoPro = daoPro;
        _daoDep = daoDep;
    }

    public IActionResult Index()
    {
        string rol = User.FindFirstValue(ClaimTypes.Role) ?? Usuario.ROL_EMPLEADO;
        string empCodigo = User.FindFirstValue(ClaimTypes.Name) ?? "";

        switch (rol)
        {
            case Usuario.ROL_ADMINISTRADOR:
                ViewBag.TotalEmpleados = _daoEmp.Listar().Count;
                ViewBag.TotalProyectos = _daoPro.Listar().Count;
                return View("Admin");

            case Usuario.ROL_RRHH:
                ViewBag.TotalEmpleados    = _daoEmp.Listar().Count;
                ViewBag.TotalDepartamentos = _daoDep.Listar().Count;
                return View("RRHH");

            case Usuario.ROL_JEFE_DEPARTAMENTO:
                ViewBag.Proyectos      = _daoPro.ListarPorEmpleado(empCodigo);
                ViewBag.TotalEmpleados = _daoEmp.Listar().Count;
                return View("Jefe");

            default:
                ViewBag.Empleado = _daoEmp.ObtenerPorCodigo(empCodigo);
                ViewBag.Proyectos = _daoPro.ListarPorEmpleado(empCodigo);
                return View("Empleado");
        }
    }
}
