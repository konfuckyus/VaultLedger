using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Repositories;
using VaultLedger.Infrastructure.Persistence;

namespace VaultLedger.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(
        AppDbContext context,
        IAccountRepository accounts,
        ICardRepository cards,
        ILedgerEntryRepository ledgerEntries,
        ITransactionRecordRepository transactionRecords,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IAccountRequestRepository accountRequests,
        ICardRequestRepository cardRequests,
        ITopUpRequestRepository topUpRequests,
        IBudgetCategoryRepository budgetCategories,
        ICategoryEligibilityRepository categoryEligibilities)
    {
        _context = context;
        Accounts = accounts;
        Cards = cards;
        LedgerEntries = ledgerEntries;
        TransactionRecords = transactionRecords;
        Users = users;
        RefreshTokens = refreshTokens;
        AccountRequests = accountRequests;
        CardRequests = cardRequests;
        TopUpRequests = topUpRequests;
        BudgetCategories = budgetCategories;
        CategoryEligibilities = categoryEligibilities;
    }

    public IAccountRepository Accounts { get; }
    public ICardRepository Cards { get; }
    public ILedgerEntryRepository LedgerEntries { get; }
    public ITransactionRecordRepository TransactionRecords { get; }
    public IUserRepository Users { get; }
    public IRefreshTokenRepository RefreshTokens { get; }
    public IAccountRequestRepository AccountRequests { get; }
    public ICardRequestRepository CardRequests { get; }
    public ITopUpRequestRepository TopUpRequests { get; }
    public IBudgetCategoryRepository BudgetCategories { get; }
    public ICategoryEligibilityRepository CategoryEligibilities { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "The account was modified by another operation. Please retry.",
                ex);
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction is not null)
                await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
            _context.ChangeTracker.Clear();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await DisposeTransactionAsync();
        _disposed = true;
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is null)
            return;

        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
