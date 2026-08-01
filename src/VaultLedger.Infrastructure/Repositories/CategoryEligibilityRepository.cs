using Microsoft.EntityFrameworkCore;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Domain.Entities;
using VaultLedger.Infrastructure.Persistence;

namespace VaultLedger.Infrastructure.Repositories;

public sealed class CategoryEligibilityRepository
    : Repository<CategoryEligibility>, ICategoryEligibilityRepository
{
    public CategoryEligibilityRepository(AppDbContext context) : base(context)
    {
    }

    public Task<CategoryEligibility?> GetByUserAndCategoryAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
        => DbSet
            .Include(x => x.User)
            .Include(x => x.Category)
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.CategoryId == categoryId,
                cancellationToken);

    public Task<bool> ExistsAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(
            x => x.UserId == userId && x.CategoryId == categoryId,
            cancellationToken);

    public async Task<IReadOnlyList<CategoryEligibility>> GetByCategoryIdAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.User)
            .Include(x => x.Category)
            .Where(x => x.CategoryId == categoryId)
            .OrderBy(x => x.User.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetCategoryIdsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.UserId == userId)
            .Select(x => x.CategoryId)
            .ToListAsync(cancellationToken);
    }
}
