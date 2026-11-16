using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinDashFrontEnd.Helpers;
using FinDashFrontEnd.Models;
using FinDashFrontEnd.Services;

namespace FinDashFrontEnd.ViewModels;

public partial class AlertRowViewModel : ObservableObject
{
    readonly Func<Guid, bool, Task> _setActive;

    public AlertRowViewModel(Guid id, bool isActive, Func<Guid, bool, Task> setActive)
    {
        Id = id;
        this.isActive = isActive;
        _setActive = setActive;
    }

    public Guid Id { get; }
    public required string Symbol { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public bool ShowDivider { get; init; }
    public required ICommand OpenCommand { get; init; }

    [ObservableProperty]
    private bool isActive;

    partial void OnIsActiveChanged(bool value) => _ = _setActive(Id, value);
}

public sealed class FiredAlertRowViewModel
{
    public required string Symbol { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public bool ShowDivider { get; init; }
    public required ICommand OpenCommand { get; init; }
}

/// <summary>Alertas activas y disparadas (RF-10, RF-11).</summary>
public partial class AlertsViewModel(IAlertService alerts, IMarketService market, ISettingsService settings) : ViewModelBase
{
    public ObservableCollection<AlertRowViewModel> Active { get; } = [];
    public ObservableCollection<FiredAlertRowViewModel> Fired { get; } = [];

    [ObservableProperty] private bool notificationsDenied;
    [ObservableProperty] private bool hasActive;
    [ObservableProperty] private bool noActive;
    [ObservableProperty] private bool hasFired;
    [ObservableProperty] private bool noFired;
    [ObservableProperty] private string activeCountText = string.Empty;
    [ObservableProperty] private bool isRefreshing;

    public async Task LoadAsync()
    {
        try
        {
            var snapshot = await market.GetSnapshotAsync();
            var list = await alerts.GetAlertsAsync();
            var fired = await alerts.GetFiredAsync();
            // TODO US-105: consultar el permiso real de notificaciones de Android 13+.
            NotificationsDenied = settings.SimulateNotificationsDenied;

            Active.Clear();
            for (var i = 0; i < list.Count; i++)
            {
                var a = list[i];
                var asset = market.GetAsset(a.AssetId);
                if (asset is null) continue;
                var q = snapshot.Quotes[asset.Id];
                var current = a.Currency == Currency.USD ? q.PriceUsd : q.PriceCrc;
                var id = a.Id;
                Active.Add(new AlertRowViewModel(a.Id, a.IsActive, alerts.SetActiveAsync)
                {
                    Symbol = asset.Symbol,
                    Title = $"{asset.ShortName} {(a.Condition == AlertCondition.Above ? "sube de" : "baja de")} {Fmt.Price(a.Threshold, a.Currency)}",
                    Subtitle = $"Hoy: {Fmt.Price(current, a.Currency)}",
                    ShowDivider = i > 0,
                    OpenCommand = new AsyncRelayCommand(() => GoToAsync($"alertedit?alertId={id}")),
                });
            }

            Fired.Clear();
            for (var i = 0; i < fired.Count; i++)
            {
                var e = fired[i];
                var asset = market.GetAsset(e.AssetId);
                if (asset is null) continue;
                var assetId = e.AssetId;
                Fired.Add(new FiredAlertRowViewModel
                {
                    Symbol = asset.Symbol,
                    Title = $"{asset.ShortName} {(e.Condition == AlertCondition.Above ? "subió de" : "bajó de")} {Fmt.Price(e.Threshold, e.Currency)}",
                    Subtitle = $"{Fmt.Ago(e.TriggeredAt)} · precio {Fmt.Price(e.PriceAtTrigger, e.Currency)}",
                    ShowDivider = i > 0,
                    OpenCommand = new AsyncRelayCommand(() => GoToAsync($"asset?id={assetId}")),
                });
            }

            HasActive = Active.Count > 0;
            NoActive = !HasActive;
            HasFired = Fired.Count > 0;
            NoFired = !HasFired;
            var activeCount = list.Count(a => a.IsActive);
            ActiveCountText = activeCount == 1 ? "1 encendida" : $"{activeCount} encendidas";
        }
        catch (Exception ex)
        {
            ErrorMessage = "No se pudieron cargar las alertas. " + ex.Message;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private Task NewAlertAsync() => GoToAsync("alertedit");

    [RelayCommand]
    private void OpenSystemSettings() => AppInfo.Current.ShowSettingsUI();
}
