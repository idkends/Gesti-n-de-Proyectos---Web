using GestionProyectosMonster.Services;
using GestionProyectosMonster.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectosMonster.Controllers;

[Authorize(Roles = Usuario.ROL_ADMINISTRADOR)]
public class UsuarioController : Controller
{
    private readonly DAOUsuario       _dao;
    private readonly DAOPerfil        _daoPerfil;
    private readonly IOpcionesService _opcService;

    public UsuarioController(DAOUsuario dao, DAOPerfil daoPerfil, IOpcionesService opcService)
    {
        _dao        = dao;
        _daoPerfil  = daoPerfil;
        _opcService = opcService;
    }

    public IActionResult Index()
    {
        ViewBag.Perfiles = _daoPerfil.Listar();
        return View(_dao.Listar());
    }

    // ==================== GESTIONAR CLAVE ====================

    [HttpGet]
    public IActionResult GestionarClave()
    {
        ViewBag.Usuarios = _dao.Listar();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GestionarClave(string codigoEmpleado, string nuevaClave, string confirmarClave)
    {
        ViewBag.Usuarios = _dao.Listar();
        try
        {
            if (nuevaClave != confirmarClave)
            {
                TempData["Error"] = "Las contraseñas no coinciden.";
                return View();
            }
            _dao.CambiarPassword(codigoEmpleado, nuevaClave);
            TempData["Exito"] = $"Contraseña de '{codigoEmpleado}' actualizada correctamente.";
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }
        return RedirectToAction(nameof(GestionarClave));
    }

    // ==================== ASIGNAR PERFIL (PickList) ====================

    [HttpGet]
    public IActionResult AsignarPerfil()
    {
        ViewBag.Usuarios = _dao.Listar();
        ViewBag.Perfiles = _daoPerfil.Listar();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AsignarPerfil(string perfilCodigo, List<string>? empleadosAsignados)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(perfilCodigo) || _daoPerfil.BuscarPorCodigo(perfilCodigo) is null)
            {
                TempData["Error"] = "Perfil inválido.";
                return RedirectToAction(nameof(AsignarPerfil));
            }
            int count = 0;
            foreach (string emp in (empleadosAsignados ?? []))
            {
                _dao.CambiarPerfil(emp, perfilCodigo);
                _opcService.InvalidarUsuario(emp);
                count++;
            }
            TempData["Exito"] = $"Perfil actualizado para {count} usuario(s).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }
        return RedirectToAction(nameof(AsignarPerfil));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Crear(string codigoEmpleado, string password, string perfilCodigo)
    {
        try
        {
            if (!_dao.ExisteEmpleado(codigoEmpleado))
            {
                TempData["Error"] = $"No existe el empleado '{codigoEmpleado}'.";
                return RedirectToAction(nameof(Index));
            }
            if (_dao.ExisteUsuario(codigoEmpleado))
            {
                TempData["Error"] = $"El empleado '{codigoEmpleado}' ya tiene usuario.";
                return RedirectToAction(nameof(Index));
            }
            if (string.IsNullOrWhiteSpace(perfilCodigo) || _daoPerfil.BuscarPorCodigo(perfilCodigo) is null)
            {
                TempData["Error"] = "Perfil inválido.";
                return RedirectToAction(nameof(Index));
            }

            var usuario = new Usuario
            {
                CodigoEmpleado = codigoEmpleado,
                PerfilCodigo = perfilCodigo,
                EstadoCodigo = "A",
                PieFirma = "Creado desde el sistema"
            };
            _dao.Insertar(usuario, password);
            TempData["Exito"] = $"Usuario '{codigoEmpleado}' creado exitosamente.";
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CambiarPerfil(string codigoEmpleado, string perfilCodigo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(perfilCodigo) || _daoPerfil.BuscarPorCodigo(perfilCodigo) is null)
            {
                TempData["Error"] = "Perfil inválido.";
                return RedirectToAction(nameof(Index));
            }
            _dao.CambiarPerfil(codigoEmpleado, perfilCodigo);
            _opcService.InvalidarUsuario(codigoEmpleado);
            TempData["Exito"] = "Perfil actualizado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CambiarEstado(string codigoEmpleado, string estado)
    {
        try
        {
            _dao.CambiarEstado(codigoEmpleado, estado);
            TempData["Exito"] = "Estado actualizado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }
}
