namespace VaultLedger.Application.Interfaces.Services;

public interface IAccountOwnershipService
{
    /// <summary>
    /// Ensures the caller owns the account or is Admin. Throws ForbiddenException otherwise.
    /// Returns the account when access is allowed.
    /// </summary>
    Task<Domain.Entities.Account> EnsureCanAccessAccountAsync(
        Guid accountId,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
