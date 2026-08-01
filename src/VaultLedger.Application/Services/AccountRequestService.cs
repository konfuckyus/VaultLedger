using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Application.Services;

public sealed class AccountRequestService : IAccountRequestService
{
    private static readonly TimeSpan DefaultCardValidity = TimeSpan.FromDays(365 * 4);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountNumberGenerator _accountNumberGenerator;
    private readonly ICardNumberGenerator _cardNumberGenerator;
    private readonly ITransactionService _transactionService;

    public AccountRequestService(
        IUnitOfWork unitOfWork,
        IAccountNumberGenerator accountNumberGenerator,
        ICardNumberGenerator cardNumberGenerator,
        ITransactionService transactionService)
    {
        _unitOfWork = unitOfWork;
        _accountNumberGenerator = accountNumberGenerator;
        _cardNumberGenerator = cardNumberGenerator;
        _transactionService = transactionService;
    }

    public async Task<AccountRequest> SubmitAccountRequestAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId is required.", nameof(categoryId));

        var category = await _unitOfWork.BudgetCategories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(BudgetCategoryDefinition), categoryId);

        if (!category.IsActive)
        {
            throw new InvalidRequestOperationException(
                "Selected budget category is not active.");
        }

        if (!category.IsSelfRequestable)
        {
            var hasEligibility = await _unitOfWork.CategoryEligibilities.ExistsAsync(
                userId, categoryId, cancellationToken);
            if (!hasEligibility)
            {
                throw new ForbiddenException(
                    "Bu kategori için hesap talebi izniniz yok.");
            }
        }

        if (await _unitOfWork.Accounts.ExistsByUserAndCategoryAsync(userId, categoryId, cancellationToken))
        {
            throw new InvalidRequestOperationException(
                "User already has an account for this category.");
        }

        var hasPending = await _unitOfWork.AccountRequests.HasPendingByUserAndCategoryAsync(
            userId, categoryId, cancellationToken);

        var request = AccountRequest.Create(userId, categoryId, hasPending);
        await _unitOfWork.AccountRequests.AddAsync(request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return request;
    }

    public Task<IReadOnlyList<AccountRequest>> GetMyAccountRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _unitOfWork.AccountRequests.GetByUserIdAsync(userId, cancellationToken);

    public Task<IReadOnlyList<AccountRequest>> GetPendingAccountRequestsAsync(
        CancellationToken cancellationToken = default)
        => _unitOfWork.AccountRequests.GetByStatusAsync(RequestStatus.Pending, cancellationToken);

    public async Task<AccountRequest> ApproveAccountRequestAsync(
        Guid requestId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (adminUserId == Guid.Empty)
            throw new ArgumentException("Admin user id is required.", nameof(adminUserId));

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var request = await _unitOfWork.AccountRequests.GetByIdAsync(requestId, cancellationToken)
                ?? throw new NotFoundException(nameof(AccountRequest), requestId);

            if (request.Status != RequestStatus.Pending)
            {
                throw new InvalidRequestOperationException(
                    $"Account request '{requestId}' is {request.Status} and cannot be reviewed.");
            }

            var category = await _unitOfWork.BudgetCategories.GetByIdAsync(
                request.CategoryId, cancellationToken)
                ?? throw new NotFoundException(nameof(BudgetCategoryDefinition), request.CategoryId);

            if (await _unitOfWork.Accounts.ExistsByUserAndCategoryAsync(
                    request.UserId, request.CategoryId, cancellationToken))
            {
                throw new InvalidRequestOperationException(
                    "User already has an account for this category.");
            }

            // Number generation must succeed BEFORE Approve — if it throws, request stays Pending.
            var accountNumber = await _accountNumberGenerator.GenerateUniqueAsync(cancellationToken);
            var account = Account.Create(
                request.UserId,
                accountNumber,
                category.Id,
                category.IsTransferable);
            await _unitOfWork.Accounts.AddAsync(account, cancellationToken);

            // Auto-issue one card labeled with the category so Spend works immediately after approve.
            var generatedCard = await _cardNumberGenerator.GenerateUniqueAsync(cancellationToken);
            var card = Card.Issue(
                account.Id,
                generatedCard.CardNumberHash,
                generatedCard.LastFourDigits,
                DateTime.UtcNow.Add(DefaultCardValidity),
                category.Name);
            await _unitOfWork.Cards.AddAsync(card, cancellationToken);

            request.Approve(adminUserId, account.Id);

            // Flush so TopUp can lock the new account row inside this same DB transaction.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (category.DefaultAllocatedAmount > 0m)
            {
                await _transactionService.TopUpInCurrentTransactionAsync(
                    account.Id,
                    category.DefaultAllocatedAmount,
                    $"account-request-approve:{request.Id:D}",
                    $"Otomatik kategori bakiyesi: {category.Name}",
                    adminUserId,
                    cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return request;
        }
        catch
        {
            await SafeRollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AccountRequest> RejectAccountRequestAsync(
        Guid requestId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (adminUserId == Guid.Empty)
            throw new ArgumentException("Admin user id is required.", nameof(adminUserId));

        var request = await _unitOfWork.AccountRequests.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundException(nameof(AccountRequest), requestId);

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
