using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FinDash.Api;
using FinDash.Api.Auth;
using FinDash.Api.Controllers;
using FinDash.Api.Data;
using FinDash.Api.Integrations;
using FinDash.Api.Services;
using FinDash.Api.Workers;
using FinDash.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var environment = builder.Environment;

services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
services.AddProblemDetails();
services.AddOpenApi();

// ---------- Base de datos (D14) ----------
// SQL Server; SQLite solo en las pruebas (Database:Provider). La configuración se lee al
// resolver, no aquí, para que las pruebas puedan cambiarla.
services.AddDbContext<FinDashDbContext>((sp, o) =>
    o.UseConfiguredDatabase(sp.GetRequiredService<IConfiguration>()));

// ---------- Autenticación JWT (D15, RNF-06) ----------
services.AddOptions<JwtOptions>()
    .BindConfiguration("Jwt")
    .PostConfigure<ILogger<JwtOptions>>((o, log) =>
    {
        if (!string.IsNullOrEmpty(o.Key) || !environment.IsDevelopment()) return;
        // Solo en desarrollo: llave al azar. Las sesiones se pierden al reiniciar la API.
        o.Key = Program.DevKey.Value;
        log.LogWarning("Jwt:Key no está configurada; se usa una llave temporal. Configúrela con: dotnet user-secrets set \"Jwt:Key\" \"...\"");
    })
    .Validate(o => o.Key is { Length: >= 32 }, "Falta Jwt:Key (mínimo 32 caracteres). Use user-secrets o la variable de entorno Jwt__Key.")
    .ValidateOnStart();

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((o, jwt) =>
    {
        o.MapInboundClaims = false; // el usuario sale del claim "sub" tal cual (contrato §1)
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Value.Issuer,
            ValidAudience = jwt.Value.Audience,
            IssuerSigningKey = jwt.Value.SigningKey,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
services.AddAuthorization();
services.AddSingleton<JwtTokenService>();
services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// Límite de intentos de registro e inicio de sesión por IP.
services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(RateLimits.Auth, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = http.RequestServices.GetRequiredService<IConfiguration>().GetValue("RateLimiting:AuthPerMinute", 10),
            Window = TimeSpan.FromMinutes(1),
        }));
});

// ---------- Fuentes de precios (D1) ----------
// Providers:Mode = Real (por defecto) o Fake. Ver ProviderMode.
services.AddSingleton(TimeProvider.System);
services.AddSingleton(sp => new CallBudget(
    sp.GetRequiredService<IConfiguration>().GetValue("CoinGecko:MaxCallsPerMinute", 25),
    sp.GetRequiredService<TimeProvider>()));
services.AddHttpClient<CoinGeckoProvider>((sp, http) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    http.BaseAddress = new Uri(config["CoinGecko:BaseUrl"] ?? "https://api.coingecko.com/api/v3/");
    http.Timeout = TimeSpan.FromSeconds(15);
    // Sin User-Agent la API pública de CoinGecko responde 403.
    http.DefaultRequestHeaders.UserAgent.ParseAdd("FinDash/1.0");
    if (config["CoinGecko:ApiKey"] is { Length: > 0 } key) http.DefaultRequestHeaders.Add("x-cg-demo-api-key", key);
});
services.AddSingleton<FakeMarketDataProvider>();
services.AddSingleton<FakeExchangeRateProvider>();
services.AddTransient<IMarketDataProvider>(sp => ProviderMode.UseFake(sp.GetRequiredService<IConfiguration>())
    ? sp.GetRequiredService<FakeMarketDataProvider>()
    : sp.GetRequiredService<CoinGeckoProvider>());
services.AddTransient<IExchangeRateProvider>(sp => ProviderMode.UseFake(sp.GetRequiredService<IConfiguration>())
    ? sp.GetRequiredService<FakeExchangeRateProvider>()
    : sp.GetRequiredService<CoinGeckoProvider>());

// ---------- Servicios de negocio ----------
services.AddMemoryCache();
services.AddSingleton<QuoteStore>();
services.AddScoped<PriceRefresher>();
services.AddScoped<FxRateLookup>();
services.AddScoped<HistoryService>();
services.AddScoped<AlertEvaluator>();
services.AddScoped<IPortfolioService, PortfolioService>();
services.AddScoped<IAlertService, AlertService>();

// ---------- Servicios en segundo plano (D3) ----------
services.AddOptions<PriceOptions>().BindConfiguration("Prices");
services.AddOptions<AlertOptions>().BindConfiguration("Alerts");
services.AddOptions<WorkerOptions>().BindConfiguration("Workers");
services.AddHostedService<PriceService>();
services.AddHostedService<AlertWorker>();

var app = builder.Build();

// Crea o actualiza la base al arrancar.
await using (var scope = app.Services.CreateAsyncScope())
    await DatabaseProvider.PrepareAsync(scope.ServiceProvider.GetRequiredService<FinDashDbContext>());

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "FinDash API v1"));
}

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));

app.Run();

/// <summary>Visible para las pruebas de integración (WebApplicationFactory).</summary>
public partial class Program
{
    internal static readonly Lazy<string> DevKey = new(() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
}
