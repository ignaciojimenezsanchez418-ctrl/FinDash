using FinDash.Core.Models;
using FinDash.Core.Services;

namespace FinDash.Tests;

/// <summary>US-402: cruce hacia arriba y abajo, no repetir, cooldown, alerta apagada y rearme.</summary>
public class AlertRuleEngineTests
{
    static readonly DateTimeOffset Now = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);
    static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(60);

    static PriceAlert Alert(AlertCondition c, decimal threshold, decimal? lastPrice = null,
        AlertState state = AlertState.Active, bool active = true, DateTimeOffset? lastTriggered = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        AssetId = "usd",
        Condition = c,
        Threshold = threshold,
        Currency = Currency.CRC,
        IsActive = active,
        State = state,
        LastPrice = lastPrice,
        LastTriggeredAt = lastTriggered,
    };

    static AlertEvaluation Eval(PriceAlert a, decimal price, DateTimeOffset? at = null) =>
        AlertRuleEngine.Evaluate(a, price, at ?? Now, Cooldown);

    [Fact]
    public void CruceHaciaArriba()
    {
        var a = Alert(AlertCondition.Above, 100_000, lastPrice: 99_000);
        Assert.Null(Eval(a, 99_999).Event);

        var r = Eval(a, 100_000);
        Assert.NotNull(r.Event);
        Assert.Equal(AlertState.Triggered, r.Alert.State);
        Assert.Equal(Now, r.Alert.LastTriggeredAt);
        Assert.Equal(100_000m, r.Event!.PriceAtTrigger);
        Assert.Equal(a.Id, r.Event.AlertId);
        Assert.Null(r.Event.DeliveredAt);
    }

    [Fact]
    public void CruceHaciaAbajo()
    {
        var a = Alert(AlertCondition.Below, 500, lastPrice: 503);
        Assert.Null(Eval(a, 500.01m).Event);
        Assert.NotNull(Eval(a, 499.80m).Event);
    }

    [Fact]
    public void PrecioQueSeQuedaArriba_NoRepite()
    {
        var a = Alert(AlertCondition.Above, 100, lastPrice: 120, state: AlertState.Triggered, lastTriggered: Now.AddHours(-5));
        var r = Eval(a, 125);

        Assert.Null(r.Event);
        Assert.Equal(AlertState.Triggered, r.Alert.State);
        Assert.Equal(125m, r.Alert.LastPrice);
    }

    [Fact]
    public void SinCruce_NoDispara()
    {
        // Ya estaba arriba en la evaluación anterior (por ejemplo, la alerta estaba apagada).
        Assert.Null(Eval(Alert(AlertCondition.Above, 100, lastPrice: 110), 115).Event);
    }

    [Fact]
    public void AlertaNueva_ConLaCondicionYaCumplida_AvisaUnaVez()
    {
        Assert.NotNull(Eval(Alert(AlertCondition.Below, 510, lastPrice: null), 503).Event);
    }

    [Fact]
    public void Rearme_CuandoElPrecioVuelveAlOtroLado()
    {
        var r = Eval(Alert(AlertCondition.Above, 100, lastPrice: 120, state: AlertState.Triggered, lastTriggered: Now), 90);

        Assert.Equal(AlertState.Rearmed, r.Alert.State);
        Assert.Null(r.Event);
    }

    [Fact]
    public void Cooldown_NoDisparaAntesDeTiempo_YDisparaDespues()
    {
        var rearmed = Alert(AlertCondition.Below, 500, lastPrice: 505, state: AlertState.Rearmed, lastTriggered: Now.AddMinutes(-30));
        Assert.Null(Eval(rearmed, 499).Event);

        // Sigue abajo cuando se cumple el cooldown: el cruce ya ocurrió, así que avisa.
        var waiting = Eval(rearmed, 499).Alert;
        Assert.NotNull(Eval(waiting, 498, Now.AddMinutes(31)).Event);
    }

    [Fact]
    public void AlertaApagada_NoDispara_PeroGuardaElPrecio()
    {
        var r = Eval(Alert(AlertCondition.Above, 100, lastPrice: 90, active: false), 200);

        Assert.Null(r.Event);
        Assert.Equal(200m, r.Alert.LastPrice);
    }

    [Fact]
    public void SinPrecio_NoCambiaNada()
    {
        var a = Alert(AlertCondition.Below, 500, lastPrice: 505);
        var r = Eval(a, 0);

        Assert.Null(r.Event);
        Assert.Equal(a, r.Alert);
    }

    [Fact]
    public void SecuenciaCompleta_UnSoloAvisoPorCruce()
    {
        // 510, 499 (avisa), 498, 497 (no repite), 505 (rearma), 499 dos horas después (avisa).
        var a = Alert(AlertCondition.Below, 500);
        var fires = 0;
        foreach (var (price, minutes) in new (decimal, int)[] { (510, 0), (499, 1), (498, 2), (497, 3), (505, 4), (499, 120) })
        {
            var r = Eval(a, price, Now.AddMinutes(minutes));
            a = r.Alert;
            if (r.Event is not null) fires++;
        }

        Assert.Equal(2, fires);
    }
}
