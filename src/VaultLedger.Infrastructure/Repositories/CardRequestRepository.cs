using Microsoft.EntityFrameworkCore;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Infrastructure.Persistence;

namespace VaultLedger.Infrastructure.Repositories;

public sealed class CardRequestRepository : Repository<CardRequest>, ICardRequestRepository
{
    public CardRequestRepository(AppDbContext context) : base(context)
    {
    }

    public Task<bool> HasPendingByUserAndAccountAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(
            x => x.UserId == userId
                 && x.AccountId == accountId
                 && x.Status == RequestStatus.Pending,
            cancellationToken);

    public async Task<IReadOnlyList<CardRequest>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.User)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CardRequest>> GetByStatusAsync(
        RequestStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.User)
            .Where(x => x.Status == status)
            .OrderBy(x => x.RequestedAt)
            .ToListAsync(cancellationToken);
    }
}
