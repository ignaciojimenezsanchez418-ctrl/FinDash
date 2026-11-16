using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinDashFrontEnd.Helpers;
using FinDashFrontEnd.Models;
using FinDashFrontEnd.Services;

namespace FinDashFrontEnd.ViewModels;

public sealed class TransactionRowViewModel
{
    public required string Symbol { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string AmountText { get; init; }
    public bool IsBuy { get; init; }
    public bool ShowDivider { get; init; }
    public required ICommand OpenCommand { get; init; }
}

public partial class PortfolioViewModel(IMarketService market, IPortfolioService portfolio, ISettingsService settings) : ViewModelBase
{
    public IList<string> TabOptions { get; } = ["Posiciones", "Movimientos"];

    public ObservableCollection<AssetRowViewModel> Holdings { get; } = [];
    public ObservableCollection<TransactionRowViewModel> Transactions { get; } = [];

    [ObservableProperty] private string valueText = string.Empty;
    [ObservableProperty] private string valueSecondaryText = string.Empty;
    [ObservableProperty] private string investedText = string.Empty;
    [ObservableProperty] private string pnlText = string.Empty;
    [ObservableProperty] private bool pnlPositive = true;

    [ObservableProperty] private int tabIndex;
    [ObservableProperty] private bool showHoldings = true;
    [ObservableProperty] private bool showTransactions;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private bool hasData;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private bool isOffline;
    [ObservableProperty] private string offlineText = string.Empty;

    partial void OnTabIndexChanged(int value) => UpdateTabs();

    void UpdateTabs()
    {
        ShowHoldings = HasData && TabIndex == 0;
        ShowTransactions = HasData && TabIndex == 1;
    }

    public async Task LoadAsync(bool force = false)
    {
        try
        {
            var snapshot = await market.GetSnapshotAsync(force);
            var summary = await portfolio.GetSummaryAsync();
            var txs = await portfolio.GetTransactionsAsync();
            var cur = settings.BaseCurrency;
            var other = cur == Currency.CRC ? Currency.USD : Currency.CRC;
            var fx = summary.FxRate;

            ValueText = Fmt.Money(summary.ValueCrc, cur, fx);
            ValueSecondaryText = $"≈ {Fmt.Money(summary.ValueCrc, other, fx)} {other}";
            InvestedText = Fmt.Money(summary.InvestedCrc, cur, fx);
            PnlText = $"{Fmt.Arrow(summary.PnlCrc)} {Fmt.Money(summary.PnlCrc, cur, fx, signed: true)} · {Fmt.Pct(summary.PnlPct)}";
            PnlPositive = summary.PnlCrc >= 0;
            IsOffline = snapshot.IsOffline;
            OfflineText = $"Sin conexión · datos de {Fmt.Ago(snapshot.UpdatedAt)}";

            Holdings.Clear();
            for (var i = 0; i < summary.Holdings.Count; i++)
            {
                var h = summary.Holdings[i];
                var id = h.Asset.Id;
                Holdings.Add(new AssetRowViewModel
                {
                    Symbol = h.Asset.Symbol,
                    Name = h.Asset.IsFx ? "Dólares" : h.Asset.Name,
                    Subtitle = Fmt.Qty(h.Quantity, h.Asset),
                    ValueText = Fmt.Money(h.ValueCrc, cur, fx),
                    ChangeText = $"{Fmt.Arrow(h.PnlCrc)} {Fmt.Pct(h.PnlPct)}",
                    IsPositive = h.PnlCrc >= 0,
                    ShowDivider = i > 0,
                    OpenCommand = new AsyncRelayCommand(() => GoToAsync($"asset?id={id}")),
                });
            }

            Transactions.Clear();
            for (var i = 0; i < txs.Count; i++)
            {
                var t = txs[i];
                var asset = market.GetAsset(t.AssetId);
                if (asset is null) continue;
                var total = t.Quantity * t.UnitPrice + (t.Type == TxType.Buy ? t.Fee : -t.Fee);
                var id = t.AssetId;
                Transactions.Add(new TransactionRowViewModel
                {
                    Symbol = asset.Symbol,
                    Title = $"{(t.Type == TxType.Buy ? "Compra" : "Venta")} · {asset.ShortName}",
                    Subtitle = $"{Fmt.Date(t.Date)} · {Fmt.Qty(t.Quantity, asset)}",
                    AmountText = Fmt.Amount(t.Type == TxType.Buy ? -total : total, t.PriceCurrency, signed: true),
                    IsBuy = t.Type == TxType.Buy,
                    ShowDivider = i > 0,
                    OpenCommand = new AsyncRelayCommand(() => GoToAsync($"asset?id={id}")),
                });
            }

            HasData = txs.Count > 0;
            IsEmpty = !HasData;
            UpdateTabs();
        }
        catch (Exception ex)
        {
            ErrorMessage = "No se pudo cargar el portafolio. " + ex.Message;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(force: true);

    [RelayCommand]
    private Task AddTransactionAsync() => GoToAsync("transaction");
}
