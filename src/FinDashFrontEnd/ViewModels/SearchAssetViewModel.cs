using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinDashFrontEnd.Models;
using FinDashFrontEnd.Services;

namespace FinDashFrontEnd.ViewModels;

public partial class SearchResultViewModel(Asset asset, bool isAdded, Func<SearchResultViewModel, Task> toggle) : ObservableObject
{
    public Asset Asset { get; } = asset;
    public string Symbol => Asset.Symbol;
    public string Name => Asset.IsFx ? "Dólar (tipo de cambio)" : Asset.Name;
    public string Detail => Asset.IsFx ? "USD/CRC · BCCR" : Asset.Symbol;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAdded))]
    private bool isAdded = isAdded;

    public bool IsNotAdded => !IsAdded;

    [RelayCommand]
    private Task ToggleAsync() => toggle(this);
}

/// <summary>Buscar activos y agregarlos a la watchlist (RF-02).</summary>
public partial class SearchAssetViewModel(IMarketService market) : ViewModelBase
{
    CancellationTokenSource? _debounce;

    public ObservableCollection<SearchResultViewModel> Results { get; } = [];

    [ObservableProperty] private string query = string.Empty;
    [ObservableProperty] private bool hasNoResults;

    public Task LoadAsync() => SearchAsync();

    partial void OnQueryChanged(string value)
    {
        _debounce?.Cancel();
        _debounce = new CancellationTokenSource();
        var token = _debounce.Token;
        _ = Task.Delay(250, token).ContinueWith(
            t => { if (!t.IsCanceled) MainThread.BeginInvokeOnMainThread(() => _ = SearchAsync()); },
            TaskScheduler.Default);
    }

    async Task SearchAsync()
    {
        var results = await market.SearchAsync(Query);
        var watchlist = await market.GetWatchlistAsync();
        Results.Clear();
        foreach (var asset in results)
            Results.Add(new SearchResultViewModel(asset, watchlist.Contains(asset.Id), ToggleAsync));
        HasNoResults = Results.Count == 0;
    }

    async Task ToggleAsync(SearchResultViewModel item)
    {
        if (item.IsAdded) await market.RemoveFromWatchlistAsync(item.Asset.Id);
        else await market.AddToWatchlistAsync(item.Asset.Id);
        item.IsAdded = !item.IsAdded;
    }
}
