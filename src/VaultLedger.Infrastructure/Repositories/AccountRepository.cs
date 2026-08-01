using Microsoft.EntityFrameworkCore;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Entities;
using VaultLedger.Infrastructure.Persistence;

namespace VaultLedger.Infrastructure.Repositories;

public sealed class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <summary>
    /// Pessimistic lock: <c>SELECT ... FOR UPDATE</c> on <c>accounts</c>, then return a tracked entity.
    /// Must be called inside an open database transaction (<c>IUnitOfWork.BeginTransactionAsync</c>).
    /// </summary>
    /// <remarks>
    /// Equivalent SQL:
    /// <code>
    /// SELECT * FROM accounts WHERE "Id" = @id FOR UPDATE;
    /// </code>
    /// </remarks>
    public async Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // FromSql + FOR UPDATE keeps the lock on the current connection until COMMIT/ROLLBACK.
        // AsTracking ensures Debit/Credit mutations are detected on SaveChanges.
        //
        // PostgreSQL SELECT * omits system columns (xmin). EF wraps FromSql in a subquery and
        // projects xmin for RowVersion — without an explicit xmin in the inner SELECT we get
        // "column v.xmin does not exist".
        return await DbSet
            .FromSqlInterpolated($"""
                SELECT *, xmin
                FROM accounts
                WHERE "Id" = {id}
                FOR UPDATE
                """)
            .AsTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Account?> GetSystemClearingAccountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(
            x => x.Id == SystemAccounts.ClearingAccountId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(x => x.Category)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByUserAndCategoryAsync(
        Guid userId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(
            x => x.UserId == userId && x.CategoryId == categoryId,
            cancellationToken);

    public Task<bool> ExistsByAccountNumberAsync(
        string accountNumber,
        CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.AccountNumber == accountNumber, cancellationToken);

    public Task<Account?> GetByAccountNumberAsync(
        string accountNumber,
        CancellationToken cancellationToken = default)
        => DbSet
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.AccountNumber == accountNumber, cancellationToken);

    public async Task<(IReadOnlyList<Account> Items, int TotalCount)> GetUserAccountsPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Category)
            .Where(x => x.AccountType == Domain.Enums.AccountType.User);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.AccountNumber.Contains(term)
                || x.User.FullName.Contains(term)
                || x.User.Email.Contains(term)
                || (x.Category != null && x.Category.Name.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
