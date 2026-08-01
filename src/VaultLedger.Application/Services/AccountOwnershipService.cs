using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Services;

public sealed class AccountOwnershipService : IAccountOwnershipService
{
    private readonly IUnitOfWork _unitOfWork;

    public AccountOwnershipService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Account> EnsureCanAccessAccountAsync(
        Guid accountId,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);

        if (!isAdmin && account.UserId != currentUserId)
            throw new ForbiddenException("You do not have access to this account.");

        return account;
    }
}
