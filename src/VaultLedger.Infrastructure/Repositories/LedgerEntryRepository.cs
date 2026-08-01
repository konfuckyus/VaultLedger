using Microsoft.EntityFrameworkCore;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Domain.Entities;
using VaultLedger.Infrastructure.Persistence;

namespace VaultLedger.Infrastructure.Repositories;

public sealed class LedgerEntryRepository : Repository<LedgerEntry>, ILedgerEntryRepository
{
    public LedgerEntryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<LedgerEntry>> GetByTransactionGroupIdAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.TransactionGroupId == groupId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }
}
