using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Repositories;

public interface ICategoryEligibilityRepository : IRepository<CategoryEligibility>
{
    Task<CategoryEligibility?> GetByUserAndCategoryAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryEligibility>> GetByCategoryIdAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetCategoryIdsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
