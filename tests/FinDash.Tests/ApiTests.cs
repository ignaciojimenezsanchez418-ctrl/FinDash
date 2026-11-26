using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinDash.Api.Auth;
using FinDash.Api.Controllers;
using FinDash.Api.Services;
using FinDash.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FinDash.Tests;

/// <summary>API con base SQLite temporal y proveedores falsos; los workers se llaman a mano.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"findash-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("Jwt:Key", "clave-solo-para-pruebas-de-integracion-0123456789");
        builder.UseSetting("Providers:Mode", "Fake");
        builder.UseSetting("Workers:Enabled", "false");
        builder.UseSetting("RateLimiting:AuthPerMinute", "1000");
    }

    /// <summary>Lo que haría PriceService: tipo de cambio y precios a la caché.</summary>
    public async Task RefreshPricesAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var refresher = scope.ServiceProvider.GetRequiredService<PriceRefresher>();
        Assert.True(await refresher.RefreshFxAsync(default));
        Assert.True(await refresher.RefreshCryptoAsync(default));
    }

    /// <summary>Lo que haría AlertWorker en su siguiente minuto.</summary>
    public async Task<int> EvaluateAlertsAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AlertEvaluator>().EvaluateAsync(default);
    }

    public QuoteStore Quotes => Services.GetRequiredService<QuoteStore>();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
            if (File.Exists(f)) File.Delete(f);
    }
}

/// <summary>Pruebas de integración de los endpoints de docs/contratos-api.md.</summary>
public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    async Task<HttpClient> SignedInClientAsync()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new Credentials($"u{Guid.NewGuid():N}@correo.com", "Clave-segura-1"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    static async Task<string?> DetailAsync(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString();

    static Transaction Tx(string asset, TxType type, decimal qty, decimal price, Currency cur, DateTime date, decimal fx = 0, decimal fee = 0) =>
        new(Guid.NewGuid(), asset, type, qty, price, cur, fee, date, fx);

    [Theory]
    [InlineData("/api/assets")]
    [InlineData("/api/market/snapshot")]
    [InlineData("/api/watchlist")]
    [InlineData("/api/portfolio/summary")]
    [InlineData("/api/portfolio/transactions")]
    [InlineData("/api/alerts")]
    [InlineData("/api/alerts/events")]
    public async Task SinToken_Responde401(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_NoPideToken()
    {
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateClient().GetAsync("/api/health")).StatusCode);
    }

    [Fact]
    public async Task RegistroEInicioDeSesion()
    {
        var client = factory.CreateClient();
        var email = $"ana{Guid.NewGuid():N}@correo.com";

        var weak = await client.PostAsJsonAsync("/api/auth/register", new Credentials(email, "corta"));
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);

        var noSymbol = await client.PostAsJsonAsync("/api/auth/register", new Credentials(email, "ClaveSegura1"));
        Assert.Equal(HttpStatusCode.BadRequest, noSymbol.StatusCode);
        Assert.Equal(AuthController.WeakPassword, await DetailAsync(noSymbol));

        var created = await client.PostAsJsonAsync("/api/auth/register", new Credentials(email, "Clave-segura-1"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new Credentials(email.ToUpperInvariant(), "Clave-segura-1"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("Ya hay una cuenta con ese correo.", await DetailAsync(duplicate));

        var wrong = await client.PostAsJsonAsync("/api/auth/login", new Credentials(email, "otra-clave"));
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal("Correo o contraseña incorrectos.", await DetailAsync(wrong));

        var ok = await client.PostAsJsonAsync("/api/auth/login", new Credentials($"  {email} ", "Clave-segura-1"));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var auth = await ok.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.Equal(email, auth!.Email);
        Assert.True(auth.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Busqueda_Bit_DevuelveBitcoin()
    {
        var client = await SignedInClientAsync();
        var results = await client.GetFromJsonAsync<List<Asset>>("/api/assets/search?q=bit", Json);
        Assert.Contains(results!, a => a.Id == "bitcoin");
        Assert.Contains((await client.GetFromJsonAsync<List<Asset>>("/api/assets", Json))!, a => a.Id == "usd" && a.Kind == AssetKind.Fiat);
    }

    [Fact]
    public async Task Watchlist_AgregarYQuitar()
    {
        var client = await SignedInClientAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync("/api/watchlist/solana", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync("/api/watchlist/solana", null)).StatusCode);
        Assert.Equal(["solana"], await client.GetFromJsonAsync<List<string>>("/api/watchlist"));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/watchlist/solana")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<string>>("/api/watchlist"))!);

        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsync("/api/watchlist/no-existe", null)).StatusCode);
    }

    [Fact]
    public async Task Mercado_SnapshotEHistorial()
    {
        await factory.RefreshPricesAsync();
        var client = await SignedInClientAsync();

        var market = await client.GetFromJsonAsync<MarketSnapshotDto>("/api/market/snapshot", Json);

        Assert.False(market!.IsOffline);
        Assert.False(market.IsStale);
        Assert.Equal(market.Fx.Buy, market.Quotes["usd"].PriceCrc);
        Assert.Equal(1m, market.Quotes["usd"].PriceUsd);
        Assert.Equal(market.Quotes["bitcoin"].PriceUsd * market.Fx.Buy, market.Quotes["bitcoin"].PriceCrc);

        var crypto = await client.GetFromJsonAsync<List<PricePointDto>>("/api/market/history/bitcoin?range=Week", Json);
        Assert.NotEmpty(crypto!);
        var fx = await client.GetFromJsonAsync<List<PricePointDto>>("/api/market/history/usd?range=Month", Json);
        Assert.All(fx!, p => Assert.Equal(1m, p.PriceUsd));
    }

    [Fact]
    public async Task Transacciones_ValidacionesYPortafolio()
    {
        await factory.RefreshPricesAsync();
        var client = await SignedInClientAsync();
        var day = new DateTime(2026, 10, 1);

        var buy = Tx("solana", TxType.Buy, 10, 90, Currency.USD, day, fx: 510, fee: 1);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/portfolio/transactions", buy, Json)).StatusCode);

        // El mismo id otra vez: responde igual y no duplica (contrato §1).
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/portfolio/transactions", buy, Json)).StatusCode);

        // M3: vender más de lo que se tiene → 422 con el mensaje del contrato.
        var oversell = await client.PostAsJsonAsync("/api/portfolio/transactions",
            Tx("solana", TxType.Sell, 11, 100, Currency.USD, day.AddDays(1), fx: 510), Json);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, oversell.StatusCode);
        Assert.Equal("No se puede vender más de lo que se tiene.", await DetailAsync(oversell));

        // Venta con fecha anterior a la compra.
        var early = await client.PostAsJsonAsync("/api/portfolio/transactions",
            Tx("solana", TxType.Sell, 1, 100, Currency.USD, day.AddDays(-1), fx: 510), Json);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, early.StatusCode);

        // Campo inválido → 400.
        var zero = await client.PostAsJsonAsync("/api/portfolio/transactions",
            Tx("solana", TxType.Buy, 0, 100, Currency.USD, day, fx: 510), Json);
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);

        // Sin fxRate: el servidor pone el tipo de cambio de esa fecha.
        var sell = await client.PostAsJsonAsync("/api/portfolio/transactions",
            Tx("solana", TxType.Sell, 4, 100, Currency.USD, day.AddDays(2)), Json);
        Assert.Equal(HttpStatusCode.Created, sell.StatusCode);
        Assert.True((await sell.Content.ReadFromJsonAsync<Transaction>(Json))!.FxRate > 0);

        Assert.Equal(6m, await client.GetFromJsonAsync<decimal>("/api/portfolio/quantity/solana"));

        var txs = await client.GetFromJsonAsync<List<Transaction>>("/api/portfolio/transactions", Json);
        Assert.Equal(2, txs!.Count);
        Assert.Equal(TxType.Sell, txs[0].Type); // más reciente primero

        var summary = await client.GetFromJsonAsync<PortfolioSummaryDto>("/api/portfolio/summary", Json);
        var h = Assert.Single(summary!.Holdings);
        Assert.Equal("solana", h.Asset.Id);
        Assert.Equal("SOL", h.Asset.Symbol);
        Assert.Equal((10 * 90 + 1) * 510m / 10, h.AvgCostCrc);

        Assert.Contains("solana", (await client.GetFromJsonAsync<List<string>>("/api/watchlist"))!);
    }

    [Fact]
    public async Task CadaUsuarioVeSoloSusDatos()
    {
        var ana = await SignedInClientAsync();
        var beto = await SignedInClientAsync();

        var tx = Tx("usd", TxType.Buy, 100, 510, Currency.CRC, new DateTime(2026, 10, 1));
        await ana.PostAsJsonAsync("/api/portfolio/transactions", tx, Json);
        var alertId = Guid.NewGuid();
        await ana.PutAsJsonAsync($"/api/alerts/{alertId}", new AlertInput(alertId, "usd", AlertCondition.Below, 500, Currency.CRC), Json);

        Assert.Empty((await beto.GetFromJsonAsync<List<Transaction>>("/api/portfolio/transactions", Json))!);
        Assert.Empty((await beto.GetFromJsonAsync<List<AlertDto>>("/api/alerts", Json))!);
        Assert.Equal(HttpStatusCode.NotFound, (await beto.GetAsync($"/api/alerts/{alertId}")).StatusCode);

        // Beto no puede editar ni apagar la alerta de Ana, ni reusar el id de su transacción.
        var hijack = await beto.PutAsJsonAsync($"/api/alerts/{alertId}", new AlertInput(alertId, "usd", AlertCondition.Above, 600, Currency.CRC), Json);
        Assert.Equal(HttpStatusCode.NotFound, hijack.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await beto.PatchAsJsonAsync($"/api/alerts/{alertId}/active", new ActiveInput(false), Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await beto.PostAsJsonAsync("/api/portfolio/transactions", tx, Json)).StatusCode);

        await beto.DeleteAsync($"/api/alerts/{alertId}");
        var anaAlert = Assert.Single((await ana.GetFromJsonAsync<List<AlertDto>>("/api/alerts", Json))!);
        Assert.Equal(500m, anaAlert.Threshold);
        Assert.True(anaAlert.IsActive);
    }

    [Fact]
    public async Task Alertas_SeDisparanUnaVez_YSeMarcanEntregadas()
    {
        await factory.RefreshPricesAsync();
        var client = await SignedInClientAsync();
        var fxBuy = factory.Quotes.Current.Fx!.Buy;

        // "Dólar baja de X" con X un poco ARRIBA del tipo de cambio actual: se cumple ya.
        var id = Guid.NewGuid();
        var saved = await client.PutAsJsonAsync($"/api/alerts/{id}", new AlertInput(id, "usd", AlertCondition.Below, fxBuy + 1, Currency.CRC), Json);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        // Una que no se cumple.
        var other = Guid.NewGuid();
        await client.PutAsJsonAsync($"/api/alerts/{other}", new AlertInput(other, "bitcoin", AlertCondition.Above, 10_000_000, Currency.USD), Json);

        await factory.EvaluateAlertsAsync();
        await factory.EvaluateAlertsAsync(); // el minuto siguiente no la repite

        var events = await client.GetFromJsonAsync<List<AlertEventDto>>("/api/alerts/events", Json);
        var ev = Assert.Single(events!);
        Assert.Equal(id, ev.AlertId);
        Assert.Equal(fxBuy, ev.PriceAtTrigger);
        Assert.Null(ev.DeliveredAt);

        var alert = await client.GetFromJsonAsync<AlertDto>($"/api/alerts/{id}", Json);
        Assert.Equal(AlertState.Triggered, alert!.State);

        // La app notifica y marca como entregado (contrato §2).
        var delivered = await client.PostAsJsonAsync("/api/alerts/events/delivered", new DeliveredInput([ev.Id]), Json);
        Assert.Equal(HttpStatusCode.NoContent, delivered.StatusCode);
        Assert.NotNull(Assert.Single((await client.GetFromJsonAsync<List<AlertEventDto>>("/api/alerts/events", Json))!).DeliveredAt);

        // Apagar y borrar: el historial se conserva.
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PatchAsJsonAsync($"/api/alerts/{id}/active", new ActiveInput(false), Json)).StatusCode);
        Assert.False((await client.GetFromJsonAsync<AlertDto>($"/api/alerts/{id}", Json))!.IsActive);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/alerts/{id}")).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<List<AlertEventDto>>("/api/alerts/events", Json))!);
    }

    [Fact]
    public async Task Alertas_EditarNoReactivaUnaApagada()
    {
        var client = await SignedInClientAsync();
        var id = Guid.NewGuid();
        await client.PutAsJsonAsync($"/api/alerts/{id}", new AlertInput(id, "bitcoin", AlertCondition.Above, 100_000, Currency.USD, IsActive: false), Json);
        await client.PutAsJsonAsync($"/api/alerts/{id}", new AlertInput(id, "bitcoin", AlertCondition.Above, 120_000, Currency.USD, IsActive: false), Json);

        var alert = await client.GetFromJsonAsync<AlertDto>($"/api/alerts/{id}", Json);
        Assert.Equal(120_000m, alert!.Threshold);
        Assert.False(alert.IsActive);
    }

    [Fact]
    public async Task Alertas_Validaciones()
    {
        var client = await SignedInClientAsync();

        var id = Guid.NewGuid();
        var zero = await client.PutAsJsonAsync($"/api/alerts/{id}", new AlertInput(id, "bitcoin", AlertCondition.Above, 0, Currency.USD), Json);
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.Equal("El monto debe ser mayor a cero.", await DetailAsync(zero));

        var usdInUsd = await client.PutAsJsonAsync($"/api/alerts/{id}", new AlertInput(id, "usd", AlertCondition.Above, 520, Currency.USD), Json);
        Assert.Equal(HttpStatusCode.BadRequest, usdInUsd.StatusCode);

        var unknown = await client.PutAsJsonAsync($"/api/alerts/{id}", new AlertInput(id, "no-existe", AlertCondition.Above, 1, Currency.USD), Json);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }
}
