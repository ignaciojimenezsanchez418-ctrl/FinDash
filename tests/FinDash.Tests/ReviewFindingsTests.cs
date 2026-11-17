using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinDash.Api.Auth;
using FinDash.Api.Controllers;
using FinDash.Api.Data;
using FinDash.Api.Services;
using FinDash.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FinDash.Tests;

/// <summary>Casos que señaló la revisión automática del PR.</summary>
public class ReviewFindingsTests
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public void FechasConDistintoOffset_SeGuardanEnOrdenReal()
    {
        var convert = (Func<DateTimeOffset, long>)new UtcTicksConverter().ConvertToProviderExpression.Compile();
        var back = (Func<long, DateTimeOffset>)new UtcTicksConverter().ConvertFromProviderExpression.Compile();

        // A ocurre antes que B, pero su hora local (18:00) es más tarde que la de B (11:00).
        var a = new DateTimeOffset(2026, 11, 17, 18, 0, 0, TimeSpan.FromHours(8));  // 10:00 UTC
        var b = new DateTimeOffset(2026, 11, 17, 11, 0, 0, TimeSpan.Zero);          // 11:00 UTC

        Assert.True(convert(a) < convert(b));
        Assert.Equal(a, back(convert(a)));                 // mismo instante
        Assert.Equal(TimeSpan.Zero, back(convert(a)).Offset);
    }

    [Fact]
    public async Task AlertaEnUsd_SeEvaluaAunqueFalteElTipoDeCambio()
    {
        // Servidor recién arrancado con el BCCR caído: hay precios cripto pero no tipo de cambio.
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new Credentials("usd@correo.com", "clave-segura-1"));
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var id = Guid.NewGuid();
        await client.PutAsJsonAsync($"/api/alerts/{id}", new AlertInput(id, "bitcoin", AlertCondition.Above, 1, Currency.USD), Json);

        await using (var scope = factory.Services.CreateAsyncScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<PriceRefresher>().RefreshCryptoAsync(default));
        Assert.Null(factory.Quotes.Current.Fx);

        Assert.Equal(1, await factory.EvaluateAlertsAsync());
    }
}
