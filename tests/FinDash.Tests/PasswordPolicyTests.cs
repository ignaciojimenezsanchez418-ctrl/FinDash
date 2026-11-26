using FinDash.Api.Controllers;

namespace FinDash.Tests;

/// <summary>Regla de contraseña del registro: 8+ caracteres con mayúscula, minúscula, número y carácter especial.</summary>
public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Clave-segura-1", true)]
    [InlineData("Ab1!xyzw", true)]          // justo 8
    [InlineData("Ab1!xyz", false)]          // 7: muy corta
    [InlineData("clave-segura-1", false)]   // sin mayúscula
    [InlineData("CLAVE-SEGURA-1", false)]   // sin minúscula
    [InlineData("Clave-segura", false)]     // sin número
    [InlineData("ClaveSegura1", false)]     // sin carácter especial
    [InlineData("Clave segura1", false)]    // el espacio no cuenta como especial
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ContraseñaFuerte(string? password, bool expected)
    {
        Assert.Equal(expected, AuthController.IsStrongPassword(password));
    }
}
