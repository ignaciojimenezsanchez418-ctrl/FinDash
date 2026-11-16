using FinDashFrontEnd.Models;

namespace FinDashFrontEnd.Services.Mock;

/// <summary>
/// Inicio de sesión de prueba: acepta cualquier correo válido con contraseña de 6+ caracteres.
/// La versión real guarda el JWT en SecureStorage (D13).
/// </summary>
public sealed class MockAuthService : IAuthService
{
    public bool IsSignedIn => Email is not null;
    public string? Email { get; private set; }

    public async Task<OperationResult> LoginAsync(string email, string password)
    {
        await MockBackend.Latency(500);
        if (password.Length < 6) return OperationResult.Fail("Correo o contraseña incorrectos.");
        Email = email.Trim();
        return OperationResult.Success;
    }

    public async Task<OperationResult> RegisterAsync(string email, string password)
    {
        await MockBackend.Latency(600);
        if (email.Trim().Equals("existe@correo.com", StringComparison.OrdinalIgnoreCase))
            return OperationResult.Fail("Ya hay una cuenta con ese correo.");
        Email = email.Trim();
        return OperationResult.Success;
    }

    public Task LogoutAsync()
    {
        Email = null;
        return Task.CompletedTask;
    }
}
