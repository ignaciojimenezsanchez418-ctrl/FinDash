using FinDashFrontEnd.Models;

namespace FinDashFrontEnd.Services;

// Contratos que la app usa para hablar con el backend. Hoy los cumplen las clases
// Mock* (datos de ejemplo en memoria); cuando la API esté lista se reemplazan por
// clientes HTTP sin tocar ViewModels ni vistas.

public interface IAuthService
{
    bool IsSignedIn { get; }
    string? Email { get; }
    Task<OperationResult> LoginAsync(string email, string password);
    Task<OperationResult> RegisterAsync(string email, string password);
    Task LogoutAsync();
}

public interface IMarketService
{
    /// <summary>Catálogo de activos conocidos (para selectores).</summary>
    IReadOnlyList<Asset> Assets { get; }

    Asset? GetAsset(string id);

    /// <summary>Precios y tipo de cambio. forceRefresh pide datos nuevos en vez de la caché.</summary>
    Task<MarketSnapshot> GetSnapshotAsync(bool forceRefresh = false);

    Task<IReadOnlyList<Asset>> SearchAsync(string query);
    Task<IReadOnlyList<PricePoint>> GetHistoryAsync(string assetId, ChartRange range);

    Task<IReadOnlyList<string>> GetWatchlistAsync();
    Task AddToWatchlistAsync(string assetId);
    Task RemoveFromWatchlistAsync(string assetId);
}

public interface IPortfolioService
{
    Task<PortfolioSummary> GetSummaryAsync();
    Task<IReadOnlyList<Transaction>> GetTransactionsAsync();
    Task<decimal> GetQuantityAsync(string assetId);
    Task<OperationResult> AddTransactionAsync(Transaction tx);
}

public interface IAlertService
{
    Task<IReadOnlyList<PriceAlert>> GetAlertsAsync();
    Task<IReadOnlyList<AlertEvent>> GetFiredAsync();
    Task<PriceAlert?> GetAsync(Guid id);
    Task<OperationResult> SaveAsync(PriceAlert alert);
    Task DeleteAsync(Guid id);
    Task SetActiveAsync(Guid id, bool active);
}

public interface ISettingsService
{
    Currency BaseCurrency { get; set; }
    int RefreshSeconds { get; set; }
    AppTheme Theme { get; set; }

    // Solo para probar estados en modo Debug.
    bool SimulateOffline { get; set; }
    bool SimulateNotificationsDenied { get; set; }
}
