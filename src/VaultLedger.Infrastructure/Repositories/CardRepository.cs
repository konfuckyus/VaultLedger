using Microsoft.EntityFrameworkCore;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Domain.Entities;
using VaultLedger.Infrastructure.Persistence;

namespace VaultLedger.Infrastructure.Repositories;

public sealed class CardRepository : Repository<Card>, ICardRepository
{
    public CardRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Card>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.AccountId == accountId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Card?> GetByCardNumberHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(x => x.CardNumberHash == hash, cancellationToken);
    }

    public Task<bool> ExistsByCardNumberHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.CardNumberHash == hash, cancellationToken);
}
