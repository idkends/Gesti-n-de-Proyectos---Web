using System.Text.RegularExpressions;

namespace GestionProyectosMonster.Services;

public static class PoliticaContrasena
{
    public static string? Validar(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "La contraseña no puede estar vacía.";
        if (password.Length < 8)
            return "La contraseña debe tener al menos 8 caracteres.";
        if (!Regex.IsMatch(password, "[A-Z]"))
            return "La contraseña debe contener al menos una letra mayúscula.";
        if (!Regex.IsMatch(password, "[a-z]"))
            return "La contraseña debe contener al menos una letra minúscula.";
        if (!Regex.IsMatch(password, "[0-9]"))
            return "La contraseña debe contener al menos un número.";
        return null;
    }
}
