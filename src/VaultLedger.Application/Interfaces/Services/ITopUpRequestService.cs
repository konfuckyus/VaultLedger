using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Services;

public interface ITopUpRequestService
{
    Task<TopUpRequest> SubmitTopUpRequestAsync(
        Guid userId,
        Guid accountId,
        decimal amount,
        string? note = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopUpRequest>> GetMyTopUpRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopUpRequest>> GetPendingTopUpRequestsAsync(
        CancellationToken cancellationToken = default);

    Task<TopUpRequest> ApproveTopUpRequestAsync(
        Guid requestId,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    Task<TopUpRequest> RejectTopUpRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default);
}
