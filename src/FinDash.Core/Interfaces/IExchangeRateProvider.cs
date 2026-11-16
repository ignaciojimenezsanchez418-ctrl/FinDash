using FinDash.Core.Models;

namespace FinDash.Core.Interfaces;

public interface IExchangeRateProvider
{
    Task<FxQuote> GetLatestAsync(CancellationToken ct = default);

    Task<IReadOnlyList<FxQuote>> GetHistoryAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}
