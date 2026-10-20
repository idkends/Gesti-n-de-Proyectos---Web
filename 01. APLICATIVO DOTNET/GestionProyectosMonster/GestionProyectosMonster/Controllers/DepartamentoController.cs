using GestionProyectosMonster.Services;
using GestionProyectosMonster.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectosMonster.Controllers;

[Authorize(Roles = Usuario.ROL_ADMINISTRADOR)]
public class DepartamentoController : Controller
{
    private readonly DAODepartamento _dao;

    public DepartamentoController(DAODepartamento dao) => _dao = dao;

    public IActionResult Index()
    {
        return View(_dao.Listar());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Guardar(Departamento dep)
    {
        if (string.IsNullOrWhiteSpace(dep.Codigo) || string.IsNullOrWhiteSpace(dep.Descripcion))
        {
            TempData["Error"] = "El código y la descripción son requeridos.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            bool existe = _dao.BuscarPorCodigo(dep.Codigo) != null;
            if (existe)
                _dao.Actualizar(dep);
            else
                _dao.Insertar(dep);

            TempData["Exito"] = existe ? "Departamento actualizado." : "Departamento creado.";
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
            string? motivo = _dao.MotivoNoPuedeEliminar(codigo);
            if (motivo != null)
            {
                TempData["Error"] = motivo;
                return RedirectToAction(nameof(Index));
            }
            _dao.Eliminar(codigo);
            TempData["Exito"] = "Departamento eliminado.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }
}
