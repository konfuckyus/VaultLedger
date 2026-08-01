using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Repositories;

public interface IBudgetCategoryRepository : IRepository<BudgetCategoryDefinition>
{
    Task<IReadOnlyList<BudgetCategoryDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BudgetCategoryDefinition>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
