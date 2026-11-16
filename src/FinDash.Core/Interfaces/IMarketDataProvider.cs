using FinDash.Core.Models;

namespace FinDash.Core.Interfaces;

public interface IMarketDataProvider
{
    Task<IReadOnlyList<MarketQuote>> GetQuotesAsync(IReadOnlyCollection<string> assetIds, CancellationToken ct = default);

    Task<IReadOnlyList<HistoryPoint>> GetHistoryAsync(string assetId, ChartRange range, CancellationToken ct = default);
}
