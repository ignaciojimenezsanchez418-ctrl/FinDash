using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinDashFrontEnd.Helpers;
using FinDashFrontEnd.Models;
using FinDashFrontEnd.Services;

namespace FinDashFrontEnd.ViewModels;

/// <summary>Crear o editar una alerta de precio o de tipo de cambio (RF-10).</summary>
public partial class AlertEditViewModel(IAlertService alerts, IMarketService market) : ViewModelBase, IQueryAttributable
{
    Guid? _alertId;
    string? _assetId;
    MarketSnapshot? _snapshot;
    bool _loaded;

    public IList<string> ConditionOptions { get; } = ["Sube de", "Baja de"];
    public IList<string> CurrencyOptions { get; } = ["USD", "CRC"];
    public IReadOnlyList<Asset> Assets => market.Assets;

    [ObservableProperty] private string title = "Nueva alerta";
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private Asset? selectedAsset;
    [ObservableProperty] private int conditionIndex;
    [ObservableProperty] private int currencyIndex;
    [ObservableProperty] private string thresholdText = string.Empty;
    [ObservableProperty] private bool canChooseCurrency = true;
    [ObservableProperty] private string currencySymbol = "$";
    [ObservableProperty] private string referenceText = string.Empty;

    Currency Currency => CurrencyIndex == 0 ? Currency.USD : Currency.CRC;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("alertId", out var a) && Guid.TryParse(a as string, out var id)) _alertId = id;
        if (query.TryGetValue("assetId", out var s) && s is string assetId) _assetId = assetId;
    }

    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;
        _snapshot = await market.GetSnapshotAsync();

        if (_alertId is { } id && await alerts.GetAsync(id) is { } alert)
        {
            Title = "Editar alerta";
            IsEditing = true;
            SelectedAsset = market.GetAsset(alert.AssetId);
            ConditionIndex = alert.Condition == AlertCondition.Above ? 0 : 1;
            CurrencyIndex = alert.Currency == Currency.USD ? 0 : 1;
            ThresholdText = Fmt.Input(alert.Threshold);
        }
        else
        {
            SelectedAsset = market.GetAsset(_assetId ?? Asset.UsdId);
        }
        UpdateReference();
    }

    partial void OnSelectedAssetChanged(Asset? value)
    {
        if (value is null) return;
        CanChooseCurrency = !value.IsFx;
        if (value.IsFx) CurrencyIndex = 1;
        UpdateReference();
    }

    partial void OnCurrencyIndexChanged(int value)
    {
        CurrencySymbol = value == 0 ? "$" : "₡";
        UpdateReference();
    }

    void UpdateReference()
    {
        if (SelectedAsset is null || _snapshot is null) return;
        var q = _snapshot.Quotes[SelectedAsset.Id];
        var price = Currency == Currency.USD ? q.PriceUsd : q.PriceCrc;
        ReferenceText = $"Hoy: {Fmt.Price(price, Currency)}";
        if (!IsEditing && string.IsNullOrEmpty(ThresholdText)) ThresholdText = Fmt.Input(price);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (SelectedAsset is null) { ErrorMessage = "Elija un activo."; return; }
        if (!Fmt.TryParse(ThresholdText, out var threshold) || threshold <= 0) { ErrorMessage = "Escriba un monto mayor a cero."; return; }

        var existing = _alertId is { } id ? await alerts.GetAsync(id) : null;
        var alert = existing ?? new PriceAlert { AssetId = SelectedAsset.Id };
        alert.AssetId = SelectedAsset.Id;
        alert.Condition = ConditionIndex == 0 ? AlertCondition.Above : AlertCondition.Below;
        alert.Threshold = threshold;
        alert.Currency = Currency;
        alert.IsActive = true;

        IsBusy = true;
        var result = await alerts.SaveAsync(alert);
        IsBusy = false;

        if (!result.Ok)
        {
            ErrorMessage = result.Error;
            return;
        }
        await GoToAsync("..");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (_alertId is not { } id) return;
        var confirm = await Shell.Current.DisplayAlertAsync("Eliminar alerta", "Esta alerta dejará de avisar.", "Eliminar", "Cancelar");
        if (!confirm) return;
        await alerts.DeleteAsync(id);
        await GoToAsync("..");
    }
}
