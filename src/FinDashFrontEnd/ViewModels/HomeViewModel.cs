using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinDashFrontEnd.Helpers;
using FinDashFrontEnd.Models;
using FinDashFrontEnd.Services;

namespace FinDashFrontEnd.ViewModels;

/// <summary>Una fila de activo en Inicio o Portafolio.</summary>
public sealed class AssetRowViewModel
{
    public required string Symbol { get; init; }
    public required string Name { get; init; }
    public required string Subtitle { get; init; }
    public required string ValueText { get; init; }
    public required string ChangeText { get; init; }
    public bool IsPositive { get; init; }
    public bool ShowDivider { get; init; }
    public required ICommand OpenCommand { get; init; }
}

public partial class HomeViewModel(IMarketService market, IPortfolioService portfolio, ISettingsService settings) : ViewModelBase
{
    public ObservableCollection<AssetRowViewModel> Items { get; } = [];

    [ObservableProperty] private string greeting = string.Empty;
    [ObservableProperty] private string totalText = string.Empty;
    [ObservableProperty] private string totalSecondaryText = string.Empty;
    [ObservableProperty] private string dayChangeText = string.Empty;
    [ObservableProperty] private bool dayChangePositive = true;

    [ObservableProperty] private string fxBuyText = string.Empty;
    [ObservableProperty] private string fxSellText = string.Empty;
    [ObservableProperty] private string fxChangeText = string.Empty;
    [ObservableProperty] private bool fxChangePositive = true;
    [ObservableProperty] private string updatedText = string.Empty;

    [ObservableProperty] private bool isOffline;
    [ObservableProperty] private string offlineText = string.Empty;

    [ObservableProperty] private bool isLoadingFirst = true;
    [ObservableProperty] private bool isLoaded;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private bool hasItems;
    [ObservableProperty] private bool isEmpty;

    public int RefreshSeconds => settings.RefreshSeconds;

    public async Task LoadAsync(bool force = false)
    {
        try
        {
            Greeting = DateTime.Now.Hour switch
            {
                < 5 => "Buenas noches",
                < 12 => "Buenos días",
                < 19 => "Buenas tardes",
                _ => "Buenas noches",
            };

            var snapshot = await market.GetSnapshotAsync(force);
            var summary = await portfolio.GetSummaryAsync();
            var watchlist = await market.GetWatchlistAsync();

            var cur = settings.BaseCurrency;
            var other = cur == Currency.CRC ? Currency.USD : Currency.CRC;
            var fx = snapshot.Fx.Buy;

            TotalText = Fmt.Money(summary.ValueCrc, cur, fx);
            TotalSecondaryText = $"≈ {Fmt.Money(summary.ValueCrc, other, fx)} {other}";
            DayChangeText = $"{Fmt.Arrow(summary.DayChangeCrc)} {Fmt.Money(summary.DayChangeCrc, cur, fx, signed: true)} · {Fmt.Pct(summary.DayChangePct)} hoy";
            DayChangePositive = summary.DayChangeCrc >= 0;

            FxBuyText = Fmt.Price(snapshot.Fx.Buy, Currency.CRC);
            FxSellText = Fmt.Price(snapshot.Fx.Sell, Currency.CRC);
            FxChangeText = Fmt.Change(snapshot.Fx.Change24hPct);
            FxChangePositive = snapshot.Fx.Change24hPct >= 0;

            var ago = Fmt.Ago(snapshot.UpdatedAt);
            UpdatedText = $"Actualizado {ago}";
            IsOffline = snapshot.IsOffline;
            OfflineText = $"Sin conexión · datos de {ago}";

            Items.Clear();
            var first = true;
            foreach (var id in watchlist)
            {
                var asset = market.GetAsset(id);
                if (asset is null || !snapshot.Quotes.TryGetValue(id, out var quote)) continue;

                var holding = summary.Holdings.FirstOrDefault(h => h.Asset.Id == id);
                Items.Add(new AssetRowViewModel
                {
                    Symbol = asset.Symbol,
                    Name = asset.IsFx ? "Dólares" : asset.Name,
                    Subtitle = holding is null ? "Sin posición" : Fmt.Qty(holding.Quantity, asset),
                    ValueText = holding is null
                        ? Fmt.Money(quote.PriceCrc, asset.IsFx ? Currency.CRC : cur, fx, decimals: quote.PriceCrc < 10_000 ? 2 : null)
                        : Fmt.Money(holding.ValueCrc, cur, fx),
                    ChangeText = Fmt.Change(quote.Change24hPct),
                    IsPositive = quote.Change24hPct >= 0,
                    ShowDivider = !first,
                    OpenCommand = new AsyncRelayCommand(() => GoToAsync($"asset?id={id}")),
                });
                first = false;
            }

            HasItems = Items.Count > 0;
            IsEmpty = !HasItems;
            IsLoadingFirst = false;
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = "No se pudieron cargar los datos. " + ex.Message;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(force: true);

    [RelayCommand]
    private Task AddAssetAsync() => GoToAsync("search");

    [RelayCommand]
    private Task OpenFxAsync() => GoToAsync($"asset?id={Asset.UsdId}");

    [RelayCommand]
    private Task OpenAlertsAsync() => GoToAsync("//main/alerts");
}
