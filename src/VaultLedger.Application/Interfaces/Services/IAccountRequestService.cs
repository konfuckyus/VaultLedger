using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Services;

public interface IAccountRequestService
{
    Task<AccountRequest> SubmitAccountRequestAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountRequest>> GetMyAccountRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountRequest>> GetPendingAccountRequestsAsync(
        CancellationToken cancellationToken = default);

    Task<AccountRequest> ApproveAccountRequestAsync(
        Guid requestId,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    Task<AccountRequest> RejectAccountRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default);
}
