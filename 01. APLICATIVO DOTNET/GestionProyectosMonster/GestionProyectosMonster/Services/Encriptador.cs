using System.Security.Cryptography;
using System.Text;

namespace GestionProyectosMonster.Services;

public static class Encriptador
{
    public static string Hash(string textoPlano)
    {
        ArgumentException.ThrowIfNullOrEmpty(textoPlano, nameof(textoPlano));
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(textoPlano));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool Verificar(string textoPlano, string hashGuardado)
    {
        if (string.IsNullOrWhiteSpace(hashGuardado)) return false;
        return Hash(textoPlano).Equals(hashGuardado.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
