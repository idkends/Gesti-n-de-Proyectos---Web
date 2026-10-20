using GestionProyectosMonster.Services;
using GestionProyectosMonster.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectosMonster.Controllers;

[Authorize(Roles = $"{Usuario.ROL_ADMINISTRADOR},{Usuario.ROL_RRHH}")]
public class EmpleadoController : Controller
{
    private readonly DAOEmpleado _dao;
    private readonly DAOUsuario  _daoUsuario;
    private readonly IWebHostEnvironment _env;

    public EmpleadoController(DAOEmpleado dao, DAOUsuario daoUsuario, IWebHostEnvironment env)
    {
        _dao        = dao;
        _daoUsuario = daoUsuario;
        _env        = env;
    }

    public IActionResult Index()
    {
        var empleados = _dao.Listar();
        return View(empleados);
    }

    [HttpGet]
    public IActionResult Organigrama()
    {
        var organigrama = _dao.ListarOrganigrama();
        return View(organigrama);
    }

    // ==================== CREAR (Wizard) ====================

    [HttpGet]
    public IActionResult Crear(string paso = "general")
    {
        ViewBag.Paso = paso;
        CargarCatalogos();
        return View(new Empleado { Codigo = _dao.GenerarCodigoSiguiente() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(Empleado empleado, string paso, IFormFile? foto)
    {
        ViewBag.Paso = paso;
        CargarCatalogos();

        if (!ModelState.IsValid)
            return View(empleado);

        try
        {
            if (!_dao.EsCargoValidoParaDepartamento(empleado.DepartamentoCodigo, empleado.CargoCodigo))
            {
                ModelState.AddModelError("", "El cargo seleccionado no pertenece al departamento indicado.");
                return View(empleado);
            }

            // Validar contraseña antes de insertar (solo Admin puede definirla)
            string rolActual  = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            string claveAdmin = "";
            bool   clavePersonalizada = false;
            if (rolActual == Usuario.ROL_ADMINISTRADOR)
            {
                claveAdmin = Request.Form["clave"].ToString().Trim();
                string confirmarClave = Request.Form["confirmarClave"].ToString().Trim();
                if (!string.IsNullOrEmpty(claveAdmin))
                {
                    if (claveAdmin != confirmarClave)
                    {
                        ModelState.AddModelError("", "Las contraseñas no coinciden.");
                        return View(empleado);
                    }
                    string? errClave = PoliticaContrasena.Validar(claveAdmin);
                    if (errClave != null)
                    {
                        ModelState.AddModelError("", errClave);
                        return View(empleado);
                    }
                    clavePersonalizada = true;
                }
            }

            if (foto != null && foto.Length > 0)
                empleado.FotoRuta = await GuardarFoto(foto, empleado.Codigo);

            _dao.Insertar(empleado);

            // Crear perfil de seguridad
            string? claveTemp = null;
            try
            {
                if (rolActual == Usuario.ROL_ADMINISTRADOR)
                {
                    string claveUsada = clavePersonalizada ? claveAdmin : $"Monster1_{empleado.Codigo}";
                    _daoUsuario.CrearPerfilBase(empleado.Codigo, claveUsada);
                    if (!clavePersonalizada)
                    {
                        claveTemp = claveUsada;
                        TempData["ClaveTemp"] = claveTemp;
                    }
                }
                else if (rolActual == Usuario.ROL_RRHH)
                {
                    claveTemp = $"Monster1_{empleado.Codigo}";
                    _daoUsuario.CrearPerfilBase(empleado.Codigo, claveTemp);
                    TempData["ClaveTemp"] = claveTemp;
                }
            }
            catch { /* No bloquear si falla el perfil; el admin lo asigna luego desde Gestión de Contraseñas */ }

            TempData["Exito"] = $"Empleado {empleado.Codigo} creado exitosamente.";
            return RedirectToAction(nameof(Editar), new { codigo = empleado.Codigo, paso = "formacion" });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
            return View(empleado);
        }
    }

    // ==================== EDITAR (Wizard) ====================

    [HttpGet]
    public IActionResult Editar(string codigo, string paso = "general")
    {
        var empleado = _dao.ObtenerPorCodigo(codigo);
        if (empleado == null) return NotFound();

        ViewBag.Paso = paso;
        CargarCatalogos();

        if (paso == "formacion")
            ViewBag.Educaciones = _dao.ListarEducacion(codigo);
        else if (paso == "familiares")
            ViewBag.Familiares = _dao.ListarFamiliares(codigo);

        return View(empleado);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Empleado empleado, string paso, IFormFile? foto)
    {
        ViewBag.Paso = paso;
        CargarCatalogos();

        if (!ModelState.IsValid)
            return View(empleado);

        try
        {
            if (!_dao.EsCargoValidoParaDepartamento(empleado.DepartamentoCodigo, empleado.CargoCodigo))
            {
                ModelState.AddModelError("", "El cargo no corresponde al departamento seleccionado.");
                return View(empleado);
            }

            if (foto != null && foto.Length > 0)
                empleado.FotoRuta = await GuardarFoto(foto, empleado.Codigo);

            _dao.Actualizar(empleado);
            TempData["Exito"] = "Empleado actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
            return View(empleado);
        }
    }

    // ==================== EDUCACIÓN (AJAX) ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AgregarEducacion(Educacion edu)
    {
        try
        {
            edu.Codigo = _dao.GenerarCodigoEducacion();
            _dao.InsertarEducacion(edu);
            TempData["Exito"] = "Título agregado correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al agregar título: {ex.Message}";
        }
        return RedirectToAction(nameof(Editar), new { codigo = edu.CodigoEmpleado, paso = "formacion" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarEducacion(string codigo, string codigoEmpleado)
    {
        try
        {
            _dao.EliminarEducacion(codigo);
            return RedirectToAction(nameof(Editar), new { codigo = codigoEmpleado, paso = "formacion" });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Editar), new { codigo = codigoEmpleado, paso = "formacion" });
        }
    }

    // ==================== FAMILIARES (AJAX) ====================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AgregarFamiliar(Familiar fam)
    {
        try
        {
            fam.Codigo = _dao.GenerarCodigoFamiliar();
            _dao.InsertarFamiliar(fam);
            TempData["Exito"] = "Familiar agregado correctamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al agregar familiar: {ex.Message}";
        }
        return RedirectToAction(nameof(Editar), new { codigo = fam.CodigoEmpleado, paso = "familiares" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarFamiliar(string codigo, string codigoEmpleado)
    {
        try
        {
            _dao.EliminarFamiliar(codigo);
            return RedirectToAction(nameof(Editar), new { codigo = codigoEmpleado, paso = "familiares" });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Editar), new { codigo = codigoEmpleado, paso = "familiares" });
        }
    }

    // ==================== HELPERS ====================

    private void CargarCatalogos()
    {
        ViewBag.Sexos         = _dao.ListarSexos();
        ViewBag.EstadosCiviles = _dao.ListarEstadosCiviles();
        ViewBag.Departamentos = _dao.ListarDepartamentos();
        ViewBag.Cargos        = _dao.ListarCargos();
        ViewBag.Parentescos   = _dao.ListarParentescos();
        ViewBag.Titulos       = _dao.ListarTitulosEducacion();
        ViewBag.Instituciones = _dao.ListarInstitucionesEducacion();
    }

    private async Task<string> GuardarFoto(IFormFile foto, string codigoEmpleado)
    {
        if (!foto.ContentType.StartsWith("image/"))
            throw new ArgumentException("Solo se aceptan imágenes.");
        if (foto.Length > 4 * 1024 * 1024)
            throw new ArgumentException("La imagen no puede superar 4 MB.");

        string ext = Path.GetExtension(foto.FileName);
        string nombre = $"{codigoEmpleado}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ext}";
        string carpeta = Path.Combine(_env.WebRootPath, "recursos", "fotos");
        Directory.CreateDirectory(carpeta);
        string ruta = Path.Combine(carpeta, nombre);
        using var stream = new FileStream(ruta, FileMode.Create);
        await foto.CopyToAsync(stream);
        return $"recursos/fotos/{nombre}";
    }
}
