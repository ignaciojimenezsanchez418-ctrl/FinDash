using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace FinDash.Api.Integrations;

public static class CoinGeckoSetup
{
    public static IHttpClientBuilder AddCoinGecko(this IServiceCollection services)
    {
        services.AddSingleton(sp => new CallBudget(
            sp.GetRequiredService<IConfiguration>().GetValue("CoinGecko:MaxCallsPerMinute", 25),
            sp.GetRequiredService<TimeProvider>()));

        var builder = services.AddHttpClient<CoinGeckoProvider>((sp, http) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            http.BaseAddress = new Uri(config["CoinGecko:BaseUrl"] ?? "https://api.coingecko.com/api/v3/");
            http.Timeout = Timeout.InfiniteTimeSpan;
            http.DefaultRequestHeaders.UserAgent.ParseAdd("FinDash/1.0");
            if (config["CoinGecko:ApiKey"] is { Length: > 0 } key) http.DefaultRequestHeaders.Add("x-cg-demo-api-key", key);
        });

        builder.AddStandardResilienceHandler().Configure((HttpStandardResilienceOptions o, IServiceProvider sp) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(CoinGeckoProvider));

            o.Retry.MaxRetryAttempts = 2;
            o.Retry.Delay = TimeSpan.FromMilliseconds(config.GetValue("CoinGecko:RetryDelayMilliseconds", 2000));
            o.Retry.BackoffType = DelayBackoffType.Exponential;
            o.Retry.ShouldRetryAfterHeader = false;
            o.Retry.OnRetry = args =>
            {
                log.LogWarning(
                    "CoinGecko falló ({Reason}); reintento {Attempt} de {Max} en {Delay} ms",
                    args.Outcome.Result is { } r ? (object)(int)r.StatusCode : args.Outcome.Exception?.GetType().Name,
                    args.AttemptNumber + 1,
                    o.Retry.MaxRetryAttempts,
                    (int)args.RetryDelay.TotalMilliseconds);
                return default;
            };
            o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(40);
        });

        return builder;
    }
}
