using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Application.Services;

public sealed class TopUpRequestService : ITopUpRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionService _transactionService;

    public TopUpRequestService(IUnitOfWork unitOfWork, ITransactionService transactionService)
    {
        _unitOfWork = unitOfWork;
        _transactionService = transactionService;
    }

    public async Task<TopUpRequest> SubmitTopUpRequestAsync(
        Guid userId,
        Guid accountId,
        decimal amount,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId is required.", nameof(accountId));

        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);

        if (account.UserId != userId)
            throw new ForbiddenException("Cannot request a top-up for an account you do not own.");

        if (account.Status != AccountStatus.Active)
        {
            throw new InvalidRequestOperationException(
                $"Account '{accountId}' is {account.Status} and cannot receive a top-up request.");
        }

        var hasPending = await _unitOfWork.TopUpRequests.HasPendingByUserAndAccountAsync(
            userId, accountId, cancellationToken);

        var request = TopUpRequest.Create(userId, accountId, amount, hasPending, note);
        await _unitOfWork.TopUpRequests.AddAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return request;
    }

    public Task<IReadOnlyList<TopUpRequest>> GetMyTopUpRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _unitOfWork.TopUpRequests.GetByUserIdAsync(userId, cancellationToken);

    public Task<IReadOnlyList<TopUpRequest>> GetPendingTopUpRequestsAsync(
        CancellationToken cancellationToken = default)
        => _unitOfWork.TopUpRequests.GetByStatusAsync(RequestStatus.Pending, cancellationToken);

    public async Task<TopUpRequest> ApproveTopUpRequestAsync(
        Guid requestId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (adminUserId == Guid.Empty)
            throw new ArgumentException("Admin user id is required.", nameof(adminUserId));

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var request = await _unitOfWork.TopUpRequests.GetByIdAsync(requestId, cancellationToken)
                ?? throw new NotFoundException(nameof(TopUpRequest), requestId);

            if (request.Status != RequestStatus.Pending)
            {
                throw new InvalidRequestOperationException(
                    $"Top-up request '{requestId}' is {request.Status} and cannot be reviewed.");
            }

            // Stable key: approve retries of the same request reuse the same idempotency key.
            var idempotencyKey = $"topup-request-approve:{request.Id:D}";
            var description = string.IsNullOrWhiteSpace(request.Note)
                ? $"Top-up request {request.Id:N}"
                : request.Note;

            var record = await _transactionService.TopUpInCurrentTransactionAsync(
                request.AccountId,
                request.Amount,
                idempotencyKey,
                description,
                adminUserId,
                cancellationToken);

            request.Approve(adminUserId, record.Id);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return request;
        }
        catch
        {
            await SafeRollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<TopUpRequest> RejectTopUpRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (adminUserId == Guid.Empty)
            throw new ArgumentException("Admin user id is required.", nameof(adminUserId));

        var request = await _unitOfWork.TopUpRequests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundException(nameof(TopUpRequest), requestId);

        request.Reject(adminUserId, reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return request;
    }

    private async Task SafeRollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        }
        catch
        {
            // Best-effort rollback; preserve the original exception.
        }
    }
}
