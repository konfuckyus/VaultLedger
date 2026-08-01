using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Application.Interfaces.Repositories;

public interface ITopUpRequestRepository : IRepository<TopUpRequest>
{
    Task<bool> HasPendingByUserAndAccountAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopUpRequest>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopUpRequest>> GetByStatusAsync(
        RequestStatus status,
        CancellationToken cancellationToken = default);
}
