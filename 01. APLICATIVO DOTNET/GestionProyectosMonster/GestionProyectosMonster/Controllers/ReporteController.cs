using GestionProyectosMonster.Services;
using GestionProyectosMonster.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectosMonster.Controllers;

[Authorize(Roles = $"{Usuario.ROL_ADMINISTRADOR},{Usuario.ROL_RRHH}")]
public class ReporteController : Controller
{
    private readonly DAOEmpleado _daoEmpleado;
    private readonly DAOUsuario  _daoUsuario;

    public ReporteController(DAOEmpleado daoEmpleado, DAOUsuario daoUsuario)
    {
        _daoEmpleado = daoEmpleado;
        _daoUsuario  = daoUsuario;
    }

    public IActionResult Index()
    {
        var empleados   = _daoEmpleado.Listar();
        var usuariosMap = _daoUsuario.Listar()
            .ToDictionary(u => u.CodigoEmpleado, u => u, StringComparer.OrdinalIgnoreCase);

        int activos    = usuariosMap.Values.Count(u => u.EstadoCodigo == "A");
        int inactivos  = usuariosMap.Values.Count(u => u.EstadoCodigo != "A");
        int sinUsuario = empleados.Count(e => !usuariosMap.ContainsKey(e.Codigo));

        ViewBag.UsuariosMap    = usuariosMap;
        ViewBag.FechaGen       = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        ViewBag.GeneradoPor    = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value ?? "Administrador";
        ViewBag.TotalEmpleados = empleados.Count;
        ViewBag.Activos        = activos;
        ViewBag.Inactivos      = inactivos;
        ViewBag.SinUsuario     = sinUsuario;

        return View(empleados);
    }
}
