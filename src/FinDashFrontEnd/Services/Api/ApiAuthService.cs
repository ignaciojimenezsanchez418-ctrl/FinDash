using System.Net;
using System.Net.Http.Json;
using FinDashFrontEnd.Models;

namespace FinDashFrontEnd.Services.Api;

/// <summary>
/// Registro e inicio de sesión contra la API (US-107). El token se guarda en SecureStorage (D13)
/// para que los demás clientes de la API lo usen.
/// </summary>
public sealed class ApiAuthService(HttpClient http) : IAuthService
{
    public const string TokenKey = "findash_token";

    public bool IsSignedIn => Email is not null;
    public string? Email { get; private set; }

    public Task<OperationResult> LoginAsync(string email, string password) =>
        SignInAsync("api/auth/login", email, password);

    public Task<OperationResult> RegisterAsync(string email, string password) =>
        SignInAsync("api/auth/register", email, password);

    public Task LogoutAsync()
    {
        Email = null;
        SecureStorage.Default.Remove(TokenKey);
        return Task.CompletedTask;
    }

    async Task<OperationResult> SignInAsync(string path, string email, string password)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(path, new { email = email.Trim(), password });
            if (!response.IsSuccessStatusCode) return OperationResult.Fail(await ErrorAsync(response));

            var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
            await SecureStorage.Default.SetAsync(TokenKey, auth.Token);
            Email = auth.Email;
            return OperationResult.Success;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return OperationResult.Fail("No se pudo conectar con el servidor. Revise su conexión.");
        }
    }

    /// <summary>La API manda el mensaje para el usuario en "detail" (ProblemDetails, contrato §1).</summary>
    static async Task<string> ErrorAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests) return "Demasiados intentos. Espere un minuto.";
        try
        {
            return (await response.Content.ReadFromJsonAsync<Problem>())?.Detail ?? GenericError;
        }
        catch (Exception)
        {
            return GenericError; // cuerpo vacío o que no es JSON
        }
    }

    const string GenericError = "No se pudo completar la solicitud. Intente de nuevo.";

    sealed record AuthResponse(string Token, string Email, DateTimeOffset ExpiresAt);

    sealed record Problem(string? Detail);
}
