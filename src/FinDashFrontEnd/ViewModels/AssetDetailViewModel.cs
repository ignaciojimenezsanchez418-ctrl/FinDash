using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinDashFrontEnd.Helpers;
using FinDashFrontEnd.Models;
using FinDashFrontEnd.Services;
using FinDashFrontEnd.Theme;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LinearGradientPaint = LiveChartsCore.SkiaSharpView.Painting.LinearGradientPaint;

namespace FinDashFrontEnd.ViewModels;

/// <summary>Detalle de un activo o del tipo de cambio, con gráfica histórica (RF-09).</summary>
public partial class AssetDetailViewModel(IMarketService market, IPortfolioService portfolio, ISettingsService settings)
    : ViewModelBase, IQueryAttributable
{
    string _assetId = Asset.UsdId;

    public IList<string> RangeOptions { get; } = ["24h", "7d", "30d", "1a"];

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string subtitle = string.Empty;
    [ObservableProperty] private string symbol = string.Empty;
    [ObservableProperty] private string priceText = string.Empty;
    [ObservableProperty] private string priceSecondaryText = string.Empty;
    [ObservableProperty] private string changeText = string.Empty;
    [ObservableProperty] private bool changePositive = true;
    [ObservableProperty] private bool isFx;
    [ObservableProperty] private string fxBuyText = string.Empty;
    [ObservableProperty] private string fxSellText = string.Empty;

    [ObservableProperty] private int rangeIndex;
    [ObservableProperty] private ISeries[] series = [];
    [ObservableProperty] private bool isChartLoading = true;
    [ObservableProperty] private string highText = string.Empty;
    [ObservableProperty] private string lowText = string.Empty;
    [ObservableProperty] private string rangeChangeText = string.Empty;
    [ObservableProperty] private bool rangeChangePositive = true;

    [ObservableProperty] private bool hasHolding;
    [ObservableProperty] private bool noHolding;
    [ObservableProperty] private string quantityText = string.Empty;
    [ObservableProperty] private string avgCostText = string.Empty;
    [ObservableProperty] private string holdingValueText = string.Empty;
    [ObservableProperty] private string pnlText = string.Empty;
    [ObservableProperty] private bool pnlPositive = true;

    [ObservableProperty] private bool inWatchlist;
    [ObservableProperty] private string watchlistText = string.Empty;
    [ObservableProperty] private bool isOffline;
    [ObservableProperty] private string updatedText = string.Empty;

    public ICartesianAxis[] XAxes { get; } = [new Axis { IsVisible = false }];
    public ICartesianAxis[] YAxes { get; } = [new Axis { IsVisible = false }];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && id is string s) _assetId = s;
    }

    public async Task LoadAsync()
    {
        var asset = market.GetAsset(_assetId);
        if (asset is null) return;

        var snapshot = await market.GetSnapshotAsync();
        var quote = snapshot.Quotes[asset.Id];
        var cur = settings.BaseCurrency;
        var fx = snapshot.Fx.Buy;

        IsFx = asset.IsFx;
        Symbol = asset.Symbol;
        Title = asset.IsFx ? "Dólar" : asset.Name;
        Subtitle = asset.IsFx ? "Tipo de cambio oficial · BCCR" : $"{asset.Symbol} · precio por unidad";

        if (asset.IsFx)
        {
            PriceText = Fmt.Price(snapshot.Fx.Buy, Currency.CRC);
            PriceSecondaryText = "Compra, por cada dólar";
            FxBuyText = Fmt.Price(snapshot.Fx.Buy, Currency.CRC);
            FxSellText = Fmt.Price(snapshot.Fx.Sell, Currency.CRC);
        }
        else
        {
            var other = cur == Currency.CRC ? Currency.USD : Currency.CRC;
            PriceText = Fmt.Money(quote.PriceCrc, cur, fx);
            PriceSecondaryText = $"≈ {Fmt.Money(quote.PriceCrc, other, fx)} {other}";
        }
        ChangeText = $"{Fmt.Change(quote.Change24hPct)} en 24 h";
        ChangePositive = quote.Change24hPct >= 0;
        IsOffline = snapshot.IsOffline;
        UpdatedText = (snapshot.IsOffline ? "Sin conexión · datos de " : "Actualizado ") + Fmt.Ago(snapshot.UpdatedAt);

        var summary = await portfolio.GetSummaryAsync();
        var holding = summary.Holdings.FirstOrDefault(h => h.Asset.Id == asset.Id);
        HasHolding = holding is not null;
        NoHolding = !HasHolding;
        if (holding is not null)
        {
            QuantityText = Fmt.Qty(holding.Quantity, asset);
            AvgCostText = asset.IsFx
                ? Fmt.Price(holding.AvgCostCrc, Currency.CRC)
                : Fmt.Money(holding.AvgCostCrc, cur, fx);
            HoldingValueText = Fmt.Money(holding.ValueCrc, cur, fx);
            PnlText = $"{Fmt.Arrow(holding.PnlCrc)} {Fmt.Money(holding.PnlCrc, cur, fx, signed: true)} · {Fmt.Pct(holding.PnlPct)}";
            PnlPositive = holding.PnlCrc >= 0;
        }

        var watchlist = await market.GetWatchlistAsync();
        SetWatchlist(watchlist.Contains(asset.Id));

        await LoadChartAsync();
    }

    partial void OnRangeIndexChanged(int value) => _ = LoadChartAsync();

    async Task LoadChartAsync()
    {
        IsChartLoading = true;
        var points = await market.GetHistoryAsync(_assetId, (ChartRange)RangeIndex);
        if (points.Count == 0)
        {
            IsChartLoading = false;
            return;
        }

        // El dólar se grafica siempre en colones; las cripto en la moneda base.
        var useUsd = !IsFx && settings.BaseCurrency == Currency.USD;
        var values = points.Select(p => useUsd ? p.PriceUsd : p.PriceCrc).ToArray();
        var currency = useUsd ? Currency.USD : Currency.CRC;

        var first = values[0];
        var last = values[^1];
        var change = first == 0 ? 0 : (last - first) / first * 100;
        HighText = "Máx. " + Fmt.Price(values.Max(), currency);
        LowText = "Mín. " + Fmt.Price(values.Min(), currency);
        RangeChangeText = Fmt.Change(change);
        RangeChangePositive = change >= 0;

        var color = ThemeManager.GetSk("PrimaryColor");
        Series =
        [
            new LineSeries<double>
            {
                Values = values.Select(v => (double)v).ToArray(),
                GeometrySize = 0,
                GeometryFill = null,
                GeometryStroke = null,
                LineSmoothness = 0.35,
                Stroke = new SolidColorPaint(color, 2.5f),
                Fill = new LinearGradientPaint(
                    [color.WithAlpha(70), color.WithAlpha(0)],
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
            },
        ];
        IsChartLoading = false;
    }

    void SetWatchlist(bool value)
    {
        InWatchlist = value;
        WatchlistText = value ? "Siguiendo" : "Seguir";
    }

    [RelayCommand]
    private async Task ToggleWatchlistAsync()
    {
        if (InWatchlist) await market.RemoveFromWatchlistAsync(_assetId);
        else await market.AddToWatchlistAsync(_assetId);
        SetWatchlist(!InWatchlist);
    }

    [RelayCommand]
    private Task CreateAlertAsync() => GoToAsync($"alertedit?assetId={_assetId}");

    [RelayCommand]
    private Task RegisterBuyAsync() => GoToAsync($"transaction?assetId={_assetId}");
}
