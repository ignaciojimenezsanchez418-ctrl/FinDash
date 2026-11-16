using FinDash.Core.Models;

namespace FinDash.Core.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioSummary> GetSummaryAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<Transaction>> GetTransactionsAsync(Guid userId, CancellationToken ct = default);

    Task<decimal> GetQuantityAsync(Guid userId, string assetId, CancellationToken ct = default);

    Task<OperationResult> AddTransactionAsync(Guid userId, Transaction tx, CancellationToken ct = default);
}
