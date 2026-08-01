using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Repositories;

public interface ITransactionRecordRepository : IRepository<TransactionRecord>
{
    /// <summary>
    /// Used by Adım 5 idempotency checks (Idempotency-Key header).
    /// </summary>
    Task<TransactionRecord?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionRecord>> GetHistoryForAccountAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
