using FinDash.Api;
using FinDash.Api.Integrations;
using FinDash.Core.Interfaces;
using FinDash.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinDash.Tests;

/// <summary>Elección de fuentes y respaldo del tipo de cambio.</summary>
public class ExchangeRateSourceTests
{
    static readonly DateTimeOffset Now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Fuente de prueba: devuelve un valor fijo o falla como un servicio caído.</summary>
    sealed class StubSource(decimal? rate) : IExchangeRateProvider
    {
        public int Calls { get; private set; }

        public Task<FxQuote> GetLatestAsync(CancellationToken ct = default)
        {
            Calls++;
            return rate is { } r
                ? Task.FromResult(new FxQuote(r, r, Now))
                : throw new HttpRequestException("503 Service Unavailable");
        }

        public async Task<IReadOnlyList<FxQuote>> GetHistoryAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
            [await GetLatestAsync(ct)];
    }

    static FallbackExchangeRateProvider Fallback(StubSource primary, StubSource backup) =>
        new(primary, backup, NullLogger<FallbackExchangeRateProvider>.Instance);

    [Fact]
    public async Task Respaldo_SiLaFuenteOficialResponde_NoUsaElRespaldo()
    {
        var bccr = new StubSource(503.12m);
        var coinGecko = new StubSource(446.89m);

        var rate = await Fallback(bccr, coinGecko).GetLatestAsync();

        Assert.Equal(503.12m, rate.Buy);
        Assert.Equal(0, coinGecko.Calls);
    }

    [Fact]
    public async Task Respaldo_SiLaFuenteOficialFalla_UsaElRespaldo()
    {
        var rates = await Fallback(new StubSource(null), new StubSource(446.89m)).GetHistoryAsync(default, default);

        Assert.Equal(446.89m, Assert.Single(rates).Buy);
    }

    [Fact]
    public async Task Respaldo_SiLasDosFallan_ElErrorSube()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() => Fallback(new StubSource(null), new StubSource(null)).GetLatestAsync());
    }

    static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value))).Build();

    [Theory]
    [InlineData(null, false)]
    [InlineData("Real", false)]
    [InlineData("Auto", false)]
    [InlineData("fake", true)]
    public void ProviderMode_SoloUsaPreciosFalsosSiSePidenAPropósito(string? mode, bool fake)
    {
        var config = mode is null ? Config() : Config(("Providers:Mode", mode));
        Assert.Equal(fake, ProviderMode.UseFake(config));
    }

    [Fact]
    public void ProviderMode_ElBccrNecesitaCorreoYToken()
    {
        Assert.False(ProviderMode.HasBccrCredentials(Config(("Bccr:Email", "a@b.cr"))));
        Assert.True(ProviderMode.HasBccrCredentials(Config(("Bccr:Email", "a@b.cr"), ("Bccr:Token", "T"))));
    }
}
