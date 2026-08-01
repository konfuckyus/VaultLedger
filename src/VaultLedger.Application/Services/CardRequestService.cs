using VaultLedger.Application.DTOs.Cards;
using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Application.Services;

public sealed class CardRequestService : ICardRequestService
{
    private static readonly TimeSpan DefaultCardValidity = TimeSpan.FromDays(365 * 4);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICardNumberGenerator _cardNumberGenerator;

    public CardRequestService(
        IUnitOfWork unitOfWork,
        ICardNumberGenerator cardNumberGenerator)
    {
        _unitOfWork = unitOfWork;
        _cardNumberGenerator = cardNumberGenerator;
    }

    public async Task<CardRequest> SubmitCardRequestAsync(
        Guid userId,
        Guid accountId,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId is required.", nameof(accountId));

        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);

        if (account.UserId != userId)
            throw new ForbiddenException("Cannot request a card for an account you do not own.");

        if (account.Status != AccountStatus.Active)
        {
            throw new InvalidRequestOperationException(
                $"Account '{accountId}' is {account.Status} and cannot receive a card request.");
        }

        // Only Pending blocks a new request — existing Active cards on the account are allowed.
        var hasPending = await _unitOfWork.CardRequests.HasPendingByUserAndAccountAsync(
            userId, accountId, cancellationToken);

        var request = CardRequest.Create(userId, accountId, hasPending, label);
        await _unitOfWork.CardRequests.AddAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return request;
    }

    public Task<IReadOnlyList<CardRequest>> GetMyCardRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _unitOfWork.CardRequests.GetByUserIdAsync(userId, cancellationToken);

    public Task<IReadOnlyList<CardRequest>> GetPendingCardRequestsAsync(
        CancellationToken cancellationToken = default)
        => _unitOfWork.CardRequests.GetByStatusAsync(RequestStatus.Pending, cancellationToken);

    public async Task<ApproveCardRequestResult> ApproveCardRequestAsync(
        Guid requestId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (adminUserId == Guid.Empty)
            throw new ArgumentException("Admin user id is required.", nameof(adminUserId));

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var request = await _unitOfWork.CardRequests.GetByIdAsync(requestId, cancellationToken)
                ?? throw new NotFoundException(nameof(CardRequest), requestId);

            if (request.Status != RequestStatus.Pending)
            {
                throw new InvalidRequestOperationException(
                    $"Card request '{requestId}' is {request.Status} and cannot be reviewed.");
            }

            // Generation must succeed BEFORE Approve — if it throws, request stays Pending.
            var generated = await _cardNumberGenerator.GenerateUniqueAsync(cancellationToken);
            var card = Card.Issue(
                request.AccountId,
                generated.CardNumberHash,
                generated.LastFourDigits,
                DateTime.UtcNow.Add(DefaultCardValidity),
                request.Label);

            await _unitOfWork.Cards.AddAsync(card, cancellationToken);
            request.Approve(adminUserId, card.Id);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Raw PAN returned once here only — never persisted or logged.
            return new ApproveCardRequestResult
            {
                CardId = card.Id,
                LastFourDigits = card.LastFourDigits,
                MaskedNumber = card.MaskedNumber,
                RawCardNumber = generated.RawCardNumber,
                Label = card.Label
            };
        }
        catch
        {
            await SafeRollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<CardRequest> RejectCardRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (adminUserId == Guid.Empty)
            throw new ArgumentException("Admin user id is required.", nameof(adminUserId));

        var request = await _unitOfWork.CardRequests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundException(nameof(CardRequest), requestId);

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
