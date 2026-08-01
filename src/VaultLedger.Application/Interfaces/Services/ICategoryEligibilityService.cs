using VaultLedger.Application.DTOs.BudgetCategories;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Services;

public interface ICategoryEligibilityService
{
    Task<CategoryEligibility> GrantAsync(
        Guid adminUserId,
        GrantCategoryEligibilityDto request,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(Guid eligibilityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryEligibility>> ListByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BudgetCategoryDefinition>> GetAvailableToUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
