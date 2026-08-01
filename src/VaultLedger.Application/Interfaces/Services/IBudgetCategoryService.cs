using VaultLedger.Application.DTOs.BudgetCategories;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Services;

public interface IBudgetCategoryService
{
    Task<IReadOnlyList<BudgetCategoryDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BudgetCategoryDefinition>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<BudgetCategoryDefinition> CreateAsync(
        CreateBudgetCategoryDto request,
        CancellationToken cancellationToken = default);

    Task<BudgetCategoryDefinition> UpdateAsync(
        Guid id,
        UpdateBudgetCategoryDto request,
        CancellationToken cancellationToken = default);
}
