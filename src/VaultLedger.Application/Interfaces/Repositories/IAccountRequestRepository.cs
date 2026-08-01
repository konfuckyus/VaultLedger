using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Application.Interfaces.Repositories;

public interface IAccountRequestRepository : IRepository<AccountRequest>
{
    Task<bool> HasPendingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> HasPendingByUserAndCategoryAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountRequest>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountRequest>> GetByStatusAsync(
        RequestStatus status,
        CancellationToken cancellationToken = default);
}
