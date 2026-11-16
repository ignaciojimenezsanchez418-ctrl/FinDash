using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinDashFrontEnd.Helpers;
using FinDashFrontEnd.Models;
using FinDashFrontEnd.Services;

namespace FinDashFrontEnd.ViewModels;

/// <summary>Formulario de compra o venta (RF-06) con validación de venta excedida (RF-07).</summary>
public partial class TransactionViewModel(IMarketService market, IPortfolioService portfolio) : ViewModelBase, IQueryAttributable
{
    MarketSnapshot? _snapshot;
    decimal _available;
    bool _loaded;

    public IList<string> TypeOptions { get; } = ["Compra", "Venta"];
    public IList<string> CurrencyOptions { get; } = ["USD", "CRC"];
    public IReadOnlyList<Asset> Assets => market.Assets;

    [ObservableProperty] private int typeIndex;
    [ObservableProperty] private Asset? selectedAsset;
    [ObservableProperty] private string quantityText = string.Empty;
    [ObservableProperty] private string unitPriceText = string.Empty;
    [ObservableProperty] private int currencyIndex;
    [ObservableProperty] private string feeText = string.Empty;
    [ObservableProperty] private DateTime date = DateTime.Today;

    [ObservableProperty] private bool canChooseCurrency = true;
    [ObservableProperty] private string currencySymbol = "$";
    [ObservableProperty] private string availableText = string.Empty;
    [ObservableProperty] private string priceHint = string.Empty;
    [ObservableProperty] private string quantityError = string.Empty;
    [ObservableProperty] private bool hasQuantityError;
    [ObservableProperty] private string totalText = "—";
    [ObservableProperty] private string saveText = "Guardar compra";

    public DateTime MaxDate => DateTime.Today;

    Currency PriceCurrency => CurrencyIndex == 0 ? Currency.USD : Currency.CRC;
    TxType Type => TypeIndex == 0 ? TxType.Buy : TxType.Sell;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("assetId", out var id) && id is string s)
            SelectedAsset = market.GetAsset(s);
    }

    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;
        _snapshot = await market.GetSnapshotAsync();
        SelectedAsset ??= market.Assets.FirstOrDefault(a => !a.IsFx);
        await OnAssetOrTypeChangedAsync();
    }

    partial void OnTypeIndexChanged(int value)
    {
        SaveText = value == 0 ? "Guardar compra" : "Guardar venta";
        _ = OnAssetOrTypeChangedAsync();
    }

    partial void OnSelectedAssetChanged(Asset? value) => _ = OnAssetOrTypeChangedAsync();

    partial void OnCurrencyIndexChanged(int value)
    {
        CurrencySymbol = value == 0 ? "$" : "₡";
        PrefillPrice();
    }

    partial void OnQuantityTextChanged(string value) => Recalculate();
    partial void OnUnitPriceTextChanged(string value) => Recalculate();
    partial void OnFeeTextChanged(string value) => Recalculate();

    async Task OnAssetOrTypeChangedAsync()
    {
        var asset = SelectedAsset;
        if (asset is null || _snapshot is null) return;

        // El dólar se compra y vende en colones.
        CanChooseCurrency = !asset.IsFx;
        if (asset.IsFx) CurrencyIndex = 1;

        _available = await portfolio.GetQuantityAsync(asset.Id);
        AvailableText = _available > 0
            ? $"Disponible: {Fmt.Qty(_available, asset)}"
            : Type == TxType.Sell ? "No hay saldo de este activo para vender." : string.Empty;

        PrefillPrice();
    }

    void PrefillPrice()
    {
        var asset = SelectedAsset;
        if (asset is null || _snapshot is null) return;

        decimal price;
        if (asset.IsFx)
            price = Type == TxType.Buy ? _snapshot.Fx.Sell : _snapshot.Fx.Buy; // se compra a venta, se vende a compra
        else
        {
            var q = _snapshot.Quotes[asset.Id];
            price = PriceCurrency == Currency.USD ? q.PriceUsd : q.PriceCrc;
        }
        UnitPriceText = Fmt.Input(price);
        PriceHint = asset.IsFx
            ? $"Tipo de cambio de {(Type == TxType.Buy ? "venta" : "compra")} hoy: {Fmt.Price(price, Currency.CRC)}"
            : $"Precio actual: {Fmt.Price(price, PriceCurrency)}";
        Recalculate();
    }

    void Recalculate()
    {
        QuantityError = string.Empty;
        if (Fmt.TryParse(QuantityText, out var qty) && Type == TxType.Sell && qty > _available && SelectedAsset is not null)
            QuantityError = $"Solo hay {Fmt.Qty(_available, SelectedAsset)} disponibles.";
        HasQuantityError = QuantityError.Length > 0;

        if (Fmt.TryParse(QuantityText, out qty) && Fmt.TryParse(UnitPriceText, out var price))
        {
            Fmt.TryParse(FeeText, out var fee);
            var total = qty * price + (Type == TxType.Buy ? fee : -fee);
            TotalText = Fmt.Amount(total, PriceCurrency);
        }
        else
        {
            TotalText = "—";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        var asset = SelectedAsset;
        if (asset is null) { ErrorMessage = "Elija un activo."; return; }
        if (!Fmt.TryParse(QuantityText, out var qty) || qty <= 0) { ErrorMessage = "Escriba una cantidad mayor a cero."; return; }
        if (!Fmt.TryParse(UnitPriceText, out var price) || price <= 0) { ErrorMessage = "Escriba un precio mayor a cero."; return; }
        var fee = 0m;
        if (!string.IsNullOrWhiteSpace(FeeText) && (!Fmt.TryParse(FeeText, out fee) || fee < 0)) { ErrorMessage = "La comisión no es válida."; return; }
        if (HasQuantityError) return;

        IsBusy = true;
        var result = await portfolio.AddTransactionAsync(new Transaction
        {
            AssetId = asset.Id,
            Type = Type,
            Quantity = qty,
            UnitPrice = price,
            PriceCurrency = PriceCurrency,
            Fee = fee,
            Date = Date,
            FxRate = _snapshot?.Fx.Buy ?? 1,
        });
        IsBusy = false;

        if (!result.Ok)
        {
            ErrorMessage = result.Error;
            return;
        }
        await GoToAsync("..");
    }
}
