using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinDash.Api.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FinDash.Api.Auth;

public sealed class JwtOptions
{
    /// <summary>
    /// Secreto para firmar los tokens (mínimo 32 caracteres). NUNCA en el repositorio (RNF-05):
    /// user-secrets en local y configuración de App Service en Azure (variable Jwt__Key).
    /// </summary>
    public string? Key { get; set; }

    public string Issuer { get; set; } = "FinDash";
    public string Audience { get; set; } = "FinDash.App";
    public int ExpirationDays { get; set; } = 7;

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key!));
}

/// <summary>Respuesta de /api/auth/login y /api/auth/register (contrato §2).</summary>
public sealed record AuthResponse(string Token, string Email, DateTimeOffset ExpiresAt);

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider time)
{
    public AuthResponse Create(User user)
    {
        var o = options.Value;
        var now = time.GetUtcNow();
        var expires = now.AddDays(o.ExpirationDays);

        var token = new JwtSecurityToken(
            issuer: o.Issuer,
            audience: o.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            ],
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(o.SigningKey, SecurityAlgorithms.HmacSha256));

        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Email, expires);
    }
}

public static class ClaimsExtensions
{
    /// <summary>El usuario sale del claim "sub", nunca del cuerpo (contrato §1).</summary>
    public static Guid UserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("El token no trae el claim sub."));
}
