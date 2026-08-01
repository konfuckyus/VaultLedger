using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Repositories;

public interface ILedgerEntryRepository : IRepository<LedgerEntry>
{
    Task<IReadOnlyList<LedgerEntry>> GetByTransactionGroupIdAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
