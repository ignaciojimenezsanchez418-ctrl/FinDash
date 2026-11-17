using FinDash.Api.Auth;
using FinDash.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FinDash.Api.Controllers;

public sealed record Credentials(string Email, string Password);

/// <summary>Registro e inicio de sesión (RF-01, US-107). Límite de intentos por IP.</summary>
[Route("api/auth")]
[AllowAnonymous]
[EnableRateLimiting(RateLimits.Auth)]
public sealed class AuthController(
    FinDashDbContext db,
    IPasswordHasher<User> hasher,
    JwtTokenService tokens,
    TimeProvider time) : ApiControllerBase
{
    public const int MinPasswordLength = 8;

    /// <summary>Crea una cuenta y devuelve el token. 201 · 409 si el correo ya existe · 400.</summary>
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(Credentials body, CancellationToken ct)
    {
        var email = Normalize(body.Email);
        if (!IsValidEmail(email)) return Error("El correo no parece válido.", 400);
        if ((body.Password ?? "").Length < MinPasswordLength)
            return Error($"La contraseña necesita al menos {MinPasswordLength} caracteres.", 400);
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return Error("Ya hay una cuenta con ese correo.", 409);

        var user = new User { Email = email, CreatedAt = time.GetUtcNow() };
        user.PasswordHash = hasher.HashPassword(user, body.Password!);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, tokens.Create(user));
    }

    /// <summary>Inicia sesión. 200 · 401 con el mismo mensaje si falla el correo o la contraseña.</summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(Credentials body, CancellationToken ct)
    {
        var email = Normalize(body.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, body.Password ?? "") == PasswordVerificationResult.Failed)
            return Error("Correo o contraseña incorrectos.", 401);

        return Ok(tokens.Create(user));
    }

    static string Normalize(string? email) => (email ?? "").Trim().ToLowerInvariant();

    static bool IsValidEmail(string email)
    {
        var at = email.IndexOf('@');
        return email.Length <= 256 && at > 0 && email.LastIndexOf('.') > at + 1 && !email.EndsWith('.') && !email.Contains(' ');
    }
}

public static class RateLimits
{
    public const string Auth = "auth";
}
