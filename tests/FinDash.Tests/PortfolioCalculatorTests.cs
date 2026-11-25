using FinDash.Core.Models;
using FinDash.Core.Services;

namespace FinDash.Tests;

/// <summary>US-303: los 9 casos del documento más los que encontró la revisión.</summary>
public class PortfolioCalculatorTests
{
    static readonly DateTime D1 = new(2026, 10, 1);
    static readonly DateTime Today = new(2026, 11, 17);

    static Transaction Tx(TxType type, string asset, decimal qty, decimal price, DateTime? date = null,
        Currency cur = Currency.CRC, decimal fee = 0, decimal fx = 1) =>
        new(Guid.NewGuid(), asset, type, qty, price, cur, fee, date ?? D1, fx);

    static Transaction Buy(string asset, decimal qty, decimal price, DateTime? date = null, Currency cur = Currency.CRC, decimal fee = 0, decimal fx = 1)
        => Tx(TxType.Buy, asset, qty, price, date, cur, fee, fx);

    static Transaction Sell(string asset, decimal qty, decimal price, DateTime? date = null, Currency cur = Currency.CRC, decimal fee = 0, decimal fx = 1)
        => Tx(TxType.Sell, asset, qty, price, date, cur, fee, fx);

    static Dictionary<string, AssetPrice> Prices(params (string Id, decimal Crc)[] p) =>
        p.ToDictionary(x => x.Id, x => new AssetPrice(x.Crc, 0));

    static PortfolioSummary Calc(IEnumerable<Transaction> txs, Dictionary<string, AssetPrice> prices, decimal fx = 500) =>
        PortfolioCalculator.Calculate(txs, prices, fx);

    [Fact]
    public void CompraSimple()
    {
        var s = Calc([Buy("bitcoin", 0.5m, 1_000_000m)], Prices(("bitcoin", 1_200_000m)));

        var h = Assert.Single(s.Holdings);
        Assert.Equal(0.5m, h.Quantity);
        Assert.Equal(1_000_000m, h.AvgCostCrc);
        Assert.Equal(500_000m, h.InvestedCrc);
        Assert.Equal(600_000m, h.ValueCrc);
        Assert.Equal(100_000m, h.PnlCrc);
        Assert.Equal(20m, h.PnlPct);
    }

    [Fact]
    public void ComprasMultiples_PromedioPonderado()
    {
        var s = Calc([Buy("solana", 1, 100), Buy("solana", 3, 200)], Prices(("solana", 200)));

        var h = Assert.Single(s.Holdings);
        Assert.Equal(4m, h.Quantity);
        Assert.Equal(175m, h.AvgCostCrc);
        Assert.Equal(700m, h.InvestedCrc);
    }

    [Fact]
    public void VentaParcial_NoCambiaElPromedio_YDejaGananciaRealizada()
    {
        var s = Calc([Buy("solana", 2, 100), Sell("solana", 1, 150)], Prices(("solana", 120)));

        var h = Assert.Single(s.Holdings);
        Assert.Equal(1m, h.Quantity);
        Assert.Equal(100m, h.AvgCostCrc);
        Assert.Equal(100m, h.InvestedCrc);
        Assert.Equal(20m, h.PnlCrc);
        Assert.Equal(50m, s.RealizedPnlCrc);
    }

    [Fact]
    public void VentaTotal_QuitaLaPosicion_YConservaLaGananciaRealizada()
    {
        var s = Calc([Buy("solana", 2, 100), Sell("solana", 2, 130)], Prices(("solana", 999)));

        Assert.Empty(s.Holdings);
        Assert.Equal(0m, s.ValueCrc);
        Assert.Equal(60m, s.RealizedPnlCrc);
    }

    [Fact]
    public void VentaExcedida_SeDetecta()
    {
        var sell = Sell("solana", 2, 100);
        Transaction[] txs = [Buy("solana", 1, 100), sell];

        Assert.Equal(sell, PortfolioCalculator.FindOversell(txs));
        Assert.Throws<InvalidOperationException>(() => Calc(txs, Prices()));
    }

    [Fact]
    public void Comisiones_SeSumanAlCosto_YSeRestanDeLaVenta()
    {
        var s = Calc(
            [Buy("ethereum", 10, 10, fee: 5), Sell("ethereum", 5, 12, fee: 2)],
            Prices(("ethereum", 10)));

        var h = Assert.Single(s.Holdings);
        Assert.Equal(10.5m, h.AvgCostCrc);       // (100 + 5) / 10
        Assert.Equal(52.5m, h.InvestedCrc);      // 5 × 10,5
        Assert.Equal(5.5m, s.RealizedPnlCrc);    // (60 − 2) − 52,5
    }

    [Fact]
    public void PrecioCero_ElPrecioDeCompraSeRechaza_YSinPrecioActualValeCero()
    {
        Assert.Equal("El precio debe ser mayor a cero.", PortfolioCalculator.Validate(Buy("bitcoin", 1, 0), Today));

        var s = Calc([Buy("bitcoin", 1, 1000)], Prices()); // sin cotización
        var h = Assert.Single(s.Holdings);
        Assert.Equal(0m, h.ValueCrc);
        Assert.Equal(-1000m, h.PnlCrc);
        Assert.Equal(-100m, h.PnlPct);
    }

    [Fact]
    public void PortafolioVacio()
    {
        var s = Calc([], Prices());

        Assert.Empty(s.Holdings);
        Assert.Equal(0m, s.ValueCrc);
        Assert.Equal(0m, s.PnlPct);
        Assert.Equal(0m, s.DayChangePct);
    }

    [Fact]
    public void ConversionCrcUsd_UsaElTipoDeCambioDeCadaTransaccion()
    {
        // 0,01 BTC a $90 000 + $2 de comisión, con tipo de cambio de ese día ₡510.
        var s = Calc(
            [Buy("bitcoin", 0.01m, 90_000m, cur: Currency.USD, fee: 2, fx: 510)],
            Prices(("bitcoin", 100_000m * 500m)),
            fx: 500);

        var h = Assert.Single(s.Holdings);
        Assert.Equal(460_020m, h.InvestedCrc);   // 902 × 510
        Assert.Equal(500_000m, h.ValueCrc);      // 0,01 × ₡50 000 000
        Assert.Equal(39_980m, h.PnlCrc);
        Assert.Equal(1_000m, h.ValueUsd);        // USD con el tipo de cambio de hoy
        Assert.Equal(1_000m, s.ValueUsd);
        Assert.Equal(79.96m, s.PnlUsd);
    }

    [Fact]
    public void AhorroEnDolares_GanaCuandoSubeElTipoDeCambio()
    {
        // Compró $1 000 a ₡510; hoy el dólar está a ₡520 → ganó ₡10 000.
        var s = Calc([Buy(Asset.UsdId, 1_000m, 510m)], Prices((Asset.UsdId, 520m)));

        var h = Assert.Single(s.Holdings);
        Assert.Equal(510_000m, h.InvestedCrc);
        Assert.Equal(10_000m, h.PnlCrc);
    }

    [Fact]
    public void CompraDespuesDeVenta_ElPromedioEsElDeLaNuevaCompra()
    {
        // "Σ compras / Σ cantidad comprada" daría 150; lo correcto es 200.
        var s = Calc(
            [
                Buy("solana", 1, 100, D1),
                Sell("solana", 1, 100, D1.AddDays(1)),
                Buy("solana", 1, 200, D1.AddDays(2)),
            ],
            Prices(("solana", 200)));

        var h = Assert.Single(s.Holdings);
        Assert.Equal(200m, h.AvgCostCrc);
        Assert.Equal(0m, h.PnlCrc);
    }

    [Fact]
    public void ElOrdenEsPorFecha_NoPorOrdenDeRegistro()
    {
        var s = Calc(
            [
                Buy("solana", 1, 200, D1.AddDays(2)),
                Sell("solana", 1, 100, D1.AddDays(1)),
                Buy("solana", 1, 100, D1),
            ],
            Prices(("solana", 200)));

        Assert.Equal(200m, Assert.Single(s.Holdings).AvgCostCrc);
    }

    [Fact]
    public void VentaConFechaAnteriorALaCompra_SeRechaza()
    {
        var sell = Sell("solana", 1, 100, new DateTime(2026, 10, 5));
        Transaction[] txs = [Buy("solana", 1, 100, new DateTime(2026, 10, 10)), sell];

        Assert.Equal(sell, PortfolioCalculator.FindOversell(txs));
    }

    [Fact]
    public void CompraYVentaElMismoDia_SeAceptanAunqueLaVentaVengaPrimero()
    {
        Assert.Null(PortfolioCalculator.FindOversell([Sell("solana", 1, 120), Buy("solana", 1, 100)]));
    }

    [Fact]
    public void VariacionDelDia()
    {
        var prices = new Dictionary<string, AssetPrice> { ["solana"] = new(1_100m, 10m) };
        var s = Calc([Buy("solana", 1, 500)], prices);

        Assert.Equal(100m, s.DayChangeCrc);   // ayer valía 1 000
        Assert.Equal(10m, s.DayChangePct);
    }

    [Theory]
    [InlineData(0, 1, 0, "La cantidad debe ser mayor a cero.")]
    [InlineData(1, 1, -1, "La comisión no puede ser negativa.")]
    public void Validaciones(decimal qty, decimal price, decimal fee, string expected)
    {
        Assert.Equal(expected, PortfolioCalculator.Validate(Buy("bitcoin", qty, price, fee: fee), Today));
    }

    [Fact]
    public void Validaciones_FechaFutura_YDolaresEnUsd()
    {
        Assert.Equal("La fecha no puede ser futura.", PortfolioCalculator.Validate(Buy("bitcoin", 1, 1, Today.AddDays(1)), Today));
        Assert.Equal("Los dólares se compran y venden en colones.",
            PortfolioCalculator.Validate(Buy(Asset.UsdId, 1, 1, cur: Currency.USD, fx: 500), Today));
        Assert.Null(PortfolioCalculator.Validate(Buy("bitcoin", 1, 1), Today));
    }
}
