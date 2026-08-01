using VaultLedger.Application.Interfaces.Repositories;

namespace VaultLedger.Application.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IAccountRepository Accounts { get; }
    ICardRepository Cards { get; }
    ILedgerEntryRepository LedgerEntries { get; }
    ITransactionRecordRepository TransactionRecords { get; }
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IAccountRequestRepository AccountRequests { get; }
    ICardRequestRepository CardRequests { get; }
    ITopUpRequestRepository TopUpRequests { get; }
    IBudgetCategoryRepository BudgetCategories { get; }
    ICategoryEligibilityRepository CategoryEligibilities { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
