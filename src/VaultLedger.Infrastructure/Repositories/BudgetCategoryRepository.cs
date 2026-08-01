using Microsoft.EntityFrameworkCore;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Domain.Entities;
using VaultLedger.Infrastructure.Persistence;

namespace VaultLedger.Infrastructure.Repositories;

public sealed class BudgetCategoryRepository : Repository<BudgetCategoryDefinition>, IBudgetCategoryRepository
{
    public BudgetCategoryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<BudgetCategoryDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .OrderByDescending(x => x.IsSystemDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BudgetCategoryDefinition>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(x => x.IsActive && x.IsSelfRequestable)
            .OrderByDescending(x => x.IsSystemDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        return DbSet.AnyAsync(
            x => x.Name == trimmed && (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }
}
