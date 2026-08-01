using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Application.Interfaces.Repositories;

public interface ICardRequestRepository : IRepository<CardRequest>
{
    Task<bool> HasPendingByUserAndAccountAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CardRequest>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CardRequest>> GetByStatusAsync(
        RequestStatus status,
        CancellationToken cancellationToken = default);
}
