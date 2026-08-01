using Microsoft.EntityFrameworkCore;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Infrastructure.Persistence;

namespace VaultLedger.Infrastructure.Repositories;

public sealed class AccountRequestRepository : Repository<AccountRequest>, IAccountRequestRepository
{
    public AccountRequestRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<AccountRequest?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.User)
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> HasPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(
            x => x.UserId == userId && x.Status == RequestStatus.Pending,
            cancellationToken);

    public Task<bool> HasPendingByUserAndCategoryAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(
            x => x.UserId == userId
                 && x.CategoryId == categoryId
                 && x.Status == RequestStatus.Pending,
            cancellationToken);

    public async Task<IReadOnlyList<AccountRequest>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.User)
            .Include(x => x.Category)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountRequest>> GetByStatusAsync(
        RequestStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.User)
            .Include(x => x.Category)
            .Where(x => x.Status == status)
            .OrderBy(x => x.RequestedAt)
            .ToListAsync(cancellationToken);
    }
}
