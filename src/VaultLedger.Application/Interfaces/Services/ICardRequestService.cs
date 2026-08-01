using VaultLedger.Application.DTOs.Cards;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Services;

public interface ICardRequestService
{
    Task<CardRequest> SubmitCardRequestAsync(
        Guid userId,
        Guid accountId,
        string? label = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CardRequest>> GetMyCardRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CardRequest>> GetPendingCardRequestsAsync(
        CancellationToken cancellationToken = default);

    Task<ApproveCardRequestResult> ApproveCardRequestAsync(
        Guid requestId,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    Task<CardRequest> RejectCardRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default);
}
