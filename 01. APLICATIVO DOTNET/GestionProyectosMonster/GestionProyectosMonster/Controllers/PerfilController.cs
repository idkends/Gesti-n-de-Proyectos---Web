using GestionProyectosMonster.Models;
using GestionProyectosMonster.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectosMonster.Controllers;

[Authorize(Roles = Usuario.ROL_ADMINISTRADOR)]
public class PerfilController : Controller
{
    private readonly DAOPerfil        _daoPerfil;
    private readonly DAOOpciones      _daoOpciones;
    private readonly IOpcionesService _opcService;

    public PerfilController(DAOPerfil daoPerfil, DAOOpciones daoOpciones, IOpcionesService opcService)
    {
        _daoPerfil   = daoPerfil;
        _daoOpciones = daoOpciones;
        _opcService  = opcService;
    }

    // ==================== CRUD PERFILES ====================

    [HttpGet]
    public IActionResult Index(string? codigo = null)
    {
        ViewBag.Perfiles      = _daoPerfil.Listar();
        ViewBag.ProximoCodigo = _daoPerfil.SiguienteCodigo();
        ViewBag.PerfilEditar  = codigo != null ? _daoPerfil.BuscarPorCodigo(codigo) : null;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(string descripcion, string? codigoOriginal)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            TempData["Error"] = "La descripción es obligatoria.";
            return RedirectToAction(nameof(Index));
        }
        if (descripcion.Length > 100)
        {
            TempData["Error"] = "La descripción no puede superar 100 caracteres.";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            if (string.IsNullOrEmpty(codigoOriginal))
            {
                var creado = _daoPerfil.Insertar(descripcion);
                TempData["Exito"] = $"Perfil {creado.Codigo} creado correctamente.";
            }
            else
            {
                _daoPerfil.Actualizar(codigoOriginal, descripcion);
                TempData["Exito"] = "Perfil actualizado correctamente.";
            }
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
        if (string.IsNullOrWhiteSpace(codigo))
        {
            TempData["Error"] = "Seleccione un perfil para eliminar.";
            return RedirectToAction(nameof(Index));
        }
        string? motivo = _daoPerfil.MotivoNoSePuedeEliminar(codigo);
        if (motivo != null)
        {
            TempData["Error"] = motivo;
            return RedirectToAction(nameof(Index));
        }
        try
        {
            _daoPerfil.Eliminar(codigo);
            TempData["Exito"] = "Perfil eliminado correctamente.";
        }
        catch
        {
            TempData["Error"] = _daoPerfil.MotivoNoSePuedeEliminar(codigo)
                ?? "No se puede eliminar el perfil porque está referenciado.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ==================== ASIGNAR OPCIONES ====================

    [HttpGet]
    public IActionResult AsignarOpciones(string? perfilCodigo = null)
    {
        ViewBag.Perfiles           = _daoPerfil.Listar();
        ViewBag.PerfilSeleccionado = perfilCodigo ?? "";
        ViewBag.TodasOpciones      = _daoOpciones.Listar();
        ViewBag.CodigosAsignados   = !string.IsNullOrEmpty(perfilCodigo)
            ? _daoOpciones.GetCodigosParaPerfil(perfilCodigo)
            : new HashSet<string>();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AsignarOpciones(string perfilCodigo, List<string>? opcionesSeleccionadas)
    {
        if (string.IsNullOrWhiteSpace(perfilCodigo))
        {
            TempData["Error"] = "Seleccione un perfil válido.";
            return RedirectToAction(nameof(AsignarOpciones));
        }
        try
        {
            _daoOpciones.AsignarOpciones(perfilCodigo, opcionesSeleccionadas ?? []);
            _opcService.InvalidarTodos();
            TempData["Exito"] = "Opciones actualizadas correctamente para el perfil seleccionado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al guardar opciones: {ex.Message}";
        }
        return RedirectToAction(nameof(AsignarOpciones), new { perfilCodigo });
    }
}
