using System.Net;
using FinDash.Api.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinDash.Tests;

public class CoinGeckoResilienceTests
{
    sealed class StubHandler(params HttpStatusCode[] codes) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var code = codes[Math.Min(Calls, codes.Length - 1)];
            Calls++;
            var body = code == HttpStatusCode.OK ? """{ "bitcoin": { "usd": 95205, "usd_24h_change": 2.1 } }""" : "";
            return Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(body) });
        }
    }

    static (CoinGeckoProvider Provider, StubHandler Stub) Build(params HttpStatusCode[] codes)
    {
        var stub = new StubHandler(codes);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["CoinGecko:RetryDelayMilliseconds"] = "10" })
            .Build());
        services.AddCoinGecko().ConfigurePrimaryHttpMessageHandler(() => stub);
        return (services.BuildServiceProvider().GetRequiredService<CoinGeckoProvider>(), stub);
    }

    [Fact]
    public async Task Ante429_EsperaYReintenta()
    {
        var (provider, stub) = Build(HttpStatusCode.TooManyRequests, HttpStatusCode.OK);

        var quotes = await provider.GetQuotesAsync(["bitcoin"]);

        Assert.Equal(95205m, Assert.Single(quotes).PriceUsd);
        Assert.Equal(2, stub.Calls);
    }

    [Fact]
    public async Task Ante503_ReintentaDosVecesYLuegoFalla()
    {
        var (provider, stub) = Build(HttpStatusCode.ServiceUnavailable);

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetQuotesAsync(["bitcoin"]));
        Assert.Equal(3, stub.Calls);
    }

    [Fact]
    public async Task Si429Persiste_DejaDeLlamarYUsaLaCache()
    {
        var (provider, stub) = Build(HttpStatusCode.TooManyRequests);

        await Assert.ThrowsAsync<RateLimitedException>(() => provider.GetQuotesAsync(["bitcoin"]));
        await Assert.ThrowsAsync<RateLimitedException>(() => provider.GetQuotesAsync(["bitcoin"]));
        Assert.Equal(3, stub.Calls);
    }

    [Fact]
    public async Task NoErrorDelCliente_NoReintenta()
    {
        var (provider, stub) = Build(HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetQuotesAsync(["bitcoin"]));
        Assert.Equal(1, stub.Calls);
    }
}
