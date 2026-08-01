using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Interfaces.Repositories;

public interface IAccountRepository : IRepository<Account>
{
    /// <summary>
    /// Loads the account with a PostgreSQL row-level lock (<c>SELECT ... FOR UPDATE</c>).
    /// Must run inside an open unit-of-work transaction.
    /// </summary>
    Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Account?> GetSystemClearingAccountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByUserAndCategoryAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// User accounts only (excludes system clearing), optional search on number or owner name.
    /// </summary>
    Task<(IReadOnlyList<Account> Items, int TotalCount)> GetUserAccountsPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);
}
