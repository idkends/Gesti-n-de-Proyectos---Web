using GestionProyectosMonster.Services;
using GestionProyectosMonster.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestionProyectosMonster.Controllers;

public class AccountController : Controller
{
    private readonly DAOUsuario _daoUsuario;

    public AccountController(DAOUsuario daoUsuario) => _daoUsuario = daoUsuario;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToDashboard();
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string codigo, string password, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Ingrese su código y contraseña.";
            return View();
        }

        try
        {
            Usuario? usuario = _daoUsuario.Autenticar(codigo.Trim().ToUpper(), password);
            if (usuario == null)
            {
                ViewBag.Error = "Código o contraseña incorrectos, o usuario inactivo.";
                return View();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name,           usuario.CodigoEmpleado),
                new(ClaimTypes.GivenName,      usuario.NombreEmpleado),
                new(ClaimTypes.Role,           usuario.RolUsuario),
                new("PerfilCodigo",            usuario.PerfilCodigo ?? ""),
                new("DepartamentoCodigo",      usuario.DepartamentoCodigo ?? "")
            };

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = false });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectionSegunRol(usuario.RolUsuario);
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Error al iniciar sesión: {ex.Message}";
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult NoAutorizado() => View();

    // ==================== CAMBIAR CLAVE ====================

    [Authorize]
    [HttpGet]
    public IActionResult CambiarClave() => View();

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CambiarClave(string claveActual, string claveNueva, string claveConfirmacion)
    {
        string codigoEmpleado = User.FindFirstValue(ClaimTypes.Name) ?? "";

        if (string.IsNullOrWhiteSpace(claveActual) || string.IsNullOrWhiteSpace(claveNueva) || string.IsNullOrWhiteSpace(claveConfirmacion))
        {
            TempData["Error"] = "Todos los campos son obligatorios.";
            return View();
        }

        if (claveNueva != claveConfirmacion)
        {
            TempData["Error"] = "La nueva contraseña y su confirmación no coinciden.";
            return View();
        }

        string? errorPolitica = PoliticaContrasena.Validar(claveNueva);
        if (errorPolitica != null)
        {
            TempData["Error"] = errorPolitica;
            return View();
        }

        Usuario? verificado = _daoUsuario.Autenticar(codigoEmpleado, claveActual);
        if (verificado == null)
        {
            TempData["Error"] = "La contraseña actual es incorrecta.";
            return View();
        }

        bool ok = _daoUsuario.CambiarPassword(codigoEmpleado, claveNueva);
        if (!ok)
        {
            TempData["Error"] = "No se pudo actualizar la contraseña. Intente nuevamente.";
            return View();
        }

        TempData["Exito"] = "Contraseña actualizada correctamente.";
        return RedirectToAction("Index", "Home");
    }

    // ==================== HELPERS ====================

    private IActionResult RedirectToDashboard()
    {
        string rol = User.FindFirstValue(ClaimTypes.Role) ?? Usuario.ROL_EMPLEADO;
        return RedirectionSegunRol(rol);
    }

    private IActionResult RedirectionSegunRol(string _) =>
        RedirectToAction("Index", "Home");
}
