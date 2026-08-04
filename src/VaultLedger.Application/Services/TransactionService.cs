using VaultLedger.Application.Exceptions;
using VaultLedger.Application.Interfaces;
using VaultLedger.Application.Interfaces.Services;
using VaultLedger.Domain.Common;
using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Application.Services;

/// <summary>
/// Closed-loop balance operations (Spend / TopUp / Refund / Transfer / Adjustment).
/// Prefer exceptions over Result&lt;T&gt;; API will map them to ProblemDetails later.
/// </summary>
public sealed class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionFailureGate _failureGate;
    private readonly IPasswordHasher _passwordHasher;

    public TransactionService(
        IUnitOfWork unitOfWork,
        ITransactionFailureGate failureGate,
        IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _failureGate = failureGate;
        _passwordHasher = passwordHasher;
    }

    // Back-compat for unit tests that only pass IUnitOfWork
    public TransactionService(IUnitOfWork unitOfWork)
        : this(unitOfWork, new NullTransactionFailureGate(), new PassThroughPasswordHasher())
    {
    }

    public TransactionService(IUnitOfWork unitOfWork, ITransactionFailureGate failureGate)
        : this(unitOfWork, failureGate, new PassThroughPasswordHasher())
    {
    }

    public async Task<TransactionRecord> SpendAsync(
        Guid userAccountId,
        Guid cardId,
        decimal amount,
        string idempotencyKey,
        string? description = null,
        string? pin = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureTransactionPinAsync(userAccountId, pin, cancellationToken);

        return await ExecuteSingleUserFlowAsync(
            TransactionType.Spend,
            userAccountId,
            amount,
            idempotencyKey,
            string.IsNullOrWhiteSpace(description) ? "Spend" : description.Trim(),
            apply: (user, system, amt) =>
            {
                user.Debit(amt);
                system.Credit(amt);
            },
            buildLedger: (groupId, user, system, amt, desc, key) =>
            (
                LedgerEntry.CreateDebit(groupId, user.Id, amt, user.Balance, desc, $"{key}:debit"),
                LedgerEntry.CreateCredit(groupId, system.Id, amt, system.Balance, desc, $"{key}:credit")
            ),
            sourceAccountId: userAccountId,
            destinationAccountId: SystemAccounts.ClearingAccountId,
            cardId: cardId,
            performedByUserId: null,
            cancellationToken: cancellationToken);
    }

    public Task<TransactionRecord> TopUpAsync(
        Guid targetAccountId,
        decimal amount,
        string idempotencyKey,
        string? description,
        Guid performedByAdminUserId,
        CancellationToken cancellationToken = default)
        => TopUpCoreAsync(
            targetAccountId,
            amount,
            idempotencyKey,
            description,
            performedByAdminUserId,
            manageTransaction: true,
            cancellationToken);

    public Task<TransactionRecord> TopUpInCurrentTransactionAsync(
        Guid targetAccountId,
        decimal amount,
        string idempotencyKey,
        string? description,
        Guid performedByAdminUserId,
        CancellationToken cancellationToken = default)
        => TopUpCoreAsync(
            targetAccountId,
            amount,
            idempotencyKey,
            description,
            performedByAdminUserId,
            manageTransaction: false,
            cancellationToken);

    private Task<TransactionRecord> TopUpCoreAsync(
        Guid targetAccountId,
        decimal amount,
        string idempotencyKey,
        string? description,
        Guid performedByAdminUserId,
        bool manageTransaction,
        CancellationToken cancellationToken)
    {
        if (performedByAdminUserId == Guid.Empty)
            throw new ArgumentException("Admin user id is required.", nameof(performedByAdminUserId));

        return ExecuteSingleUserFlowAsync(
            TransactionType.TopUp,
            targetAccountId,
            amount,
            idempotencyKey,
            string.IsNullOrWhiteSpace(description) ? "Top-up" : description.Trim(),
            apply: (user, system, amt) =>
            {
                system.Debit(amt);
                user.Credit(amt);
            },
            buildLedger: (groupId, user, system, amt, desc, key) =>
            (
                LedgerEntry.CreateDebit(groupId, system.Id, amt, system.Balance, desc, $"{key}:debit"),
                LedgerEntry.CreateCredit(groupId, user.Id, amt, user.Balance, desc, $"{key}:credit")
            ),
            sourceAccountId: SystemAccounts.ClearingAccountId,
            destinationAccountId: targetAccountId,
            cardId: null,
            performedByUserId: performedByAdminUserId,
            manageTransaction: manageTransaction,
            cancellationToken: cancellationToken);
    }

    public Task<TransactionRecord> RefundAsync(
        Guid userAccountId,
        decimal amount,
        string idempotencyKey,
        string? description = null,
        CancellationToken cancellationToken = default)
        => ExecuteSingleUserFlowAsync(
            TransactionType.Refund,
            userAccountId,
            amount,
            idempotencyKey,
            string.IsNullOrWhiteSpace(description) ? "Refund" : description.Trim(),
            apply: (user, system, amt) =>
            {
                system.Debit(amt);
                user.Credit(amt);
            },
            buildLedger: (groupId, user, system, amt, desc, key) =>
            (
                LedgerEntry.CreateDebit(groupId, system.Id, amt, system.Balance, desc, $"{key}:debit"),
                LedgerEntry.CreateCredit(groupId, user.Id, amt, user.Balance, desc, $"{key}:credit")
            ),
            sourceAccountId: SystemAccounts.ClearingAccountId,
            destinationAccountId: userAccountId,
            cardId: null,
            performedByUserId: null,
            cancellationToken: cancellationToken);

    public Task<TransactionRecord> AdjustmentAsync(
        Guid targetAccountId,
        decimal amount,
        AdjustmentDirection direction,
        string reason,
        string idempotencyKey,
        Guid performedByAdminUserId,
        CancellationToken cancellationToken = default)
    {
        if (performedByAdminUserId == Guid.Empty)
            throw new ArgumentException("Admin user id is required.", nameof(performedByAdminUserId));

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var trimmedReason = reason.Trim();
        var isIncrease = direction == AdjustmentDirection.Increase;

        return ExecuteSingleUserFlowAsync(
            TransactionType.Adjustment,
            targetAccountId,
            amount,
            idempotencyKey,
            trimmedReason,
            apply: (user, system, amt) =>
            {
                if (isIncrease)
                {
                    system.Debit(amt);
                    user.Credit(amt);
                }
                else
                {
                    user.Debit(amt);
                    system.Credit(amt);
                }
            },
            buildLedger: (groupId, user, system, amt, desc, key) =>
            {
                if (isIncrease)
                {
                    return (
                        LedgerEntry.CreateDebit(groupId, system.Id, amt, system.Balance, desc, $"{key}:debit"),
                        LedgerEntry.CreateCredit(groupId, user.Id, amt, user.Balance, desc, $"{key}:credit"));
                }

                return (
                    LedgerEntry.CreateDebit(groupId, user.Id, amt, user.Balance, desc, $"{key}:debit"),
                    LedgerEntry.CreateCredit(groupId, system.Id, amt, system.Balance, desc, $"{key}:credit"));
            },
            sourceAccountId: isIncrease ? SystemAccounts.ClearingAccountId : targetAccountId,
            destinationAccountId: isIncrease ? targetAccountId : SystemAccounts.ClearingAccountId,
            cardId: null,
            performedByUserId: performedByAdminUserId,
            cancellationToken: cancellationToken);
    }

    public async Task<TransactionRecord> TransferAsync(
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        string idempotencyKey,
        string? description = null,
        string? pin = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (sourceAccountId == Guid.Empty)
            throw new ArgumentException("Source account id is required.", nameof(sourceAccountId));
        if (destinationAccountId == Guid.Empty)
            throw new ArgumentException("Destination account id is required.", nameof(destinationAccountId));
        if (sourceAccountId == destinationAccountId)
            throw new InvalidAccountOperationException("Source and destination accounts must differ for transfers.");
        if (amount <= 0m)
            throw new InvalidAmountException("Amount must be greater than zero.");

        await EnsureTransactionPinAsync(sourceAccountId, pin, cancellationToken);

        var key = idempotencyKey.Trim();
        var desc = string.IsNullOrWhiteSpace(description) ? "Transfer" : description.Trim();

        var existing = await FindCompletedOrRejectInFlightAsync(key, cancellationToken);
        if (existing is not null)
            return existing;

        var transactionGroupId = Guid.NewGuid();

        try
        {
            // 1) Idempotency (above) — before any transaction / lock.
            // 2) BeginTransaction
            // 3) Lock by Guid.CompareTo ascending (not by source/destination role)
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var (source, destination) = await LockTransferAccountsInIdOrderAsync(
                sourceAccountId,
                destinationAccountId,
                cancellationToken);

            EnsureAccountActiveForTransfer(source);
            EnsureAccountActiveForTransfer(destination);

            if (source.AccountType != AccountType.User || destination.AccountType != AccountType.User)
                throw new InvalidAccountOperationException("Transfers are only allowed between user accounts.");

            if (!source.IsTransferable)
            {
                throw new InvalidAccountOperationException(
                    "This account category does not allow transfers.");
            }

            // Debit/Credit order follows business roles (independent of lock acquisition order).
            source.Debit(amount);
            destination.Credit(amount);

            var debit = LedgerEntry.CreateDebit(
                transactionGroupId, source.Id, amount, source.Balance, desc, $"{key}:debit");
            var credit = LedgerEntry.CreateCredit(
                transactionGroupId, destination.Id, amount, destination.Balance, desc, $"{key}:credit");

            var record = TransactionRecord.Create(
                TransactionType.Transfer,
                sourceAccountId,
                destinationAccountId,
                amount,
                transactionGroupId,
                key);
            record.MarkCompleted();

            await _unitOfWork.LedgerEntries.AddAsync(debit, cancellationToken);
            await _unitOfWork.LedgerEntries.AddAsync(credit, cancellationToken);
            await _unitOfWork.TransactionRecords.AddAsync(record, cancellationToken);
            _unitOfWork.Accounts.Update(source);
            _unitOfWork.Accounts.Update(destination);

            // Test seam: integration tests can throw here to prove rollback leaves no commit.
            await _failureGate.BeforeCommitAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return record;
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            await SafeRollbackAsync(cancellationToken);
            return await ResolveIdempotentRaceAsync(key, cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            await SafeRollbackAsync(cancellationToken);
            await TryPersistFailedAsync(
                TransactionType.Transfer,
                sourceAccountId,
                destinationAccountId,
                amount,
                transactionGroupId,
                key,
                cardId: null,
                performedByUserId: null,
                description: desc,
                cancellationToken);
            throw;
        }
        catch (Exception)
        {
            await SafeRollbackAsync(cancellationToken);
            await TryPersistFailedAsync(
                TransactionType.Transfer,
                sourceAccountId,
                destinationAccountId,
                amount,
                transactionGroupId,
                key,
                cardId: null,
                performedByUserId: null,
                description: desc,
                cancellationToken);
            throw;
        }
    }

    private Task<TransactionRecord> ExecuteSingleUserFlowAsync(
        TransactionType type,
        Guid userAccountId,
        decimal amount,
        string idempotencyKey,
        string description,
        Action<Account, Account, decimal> apply,
        Func<Guid, Account, Account, decimal, string, string, (LedgerEntry Debit, LedgerEntry Credit)> buildLedger,
        Guid sourceAccountId,
        Guid destinationAccountId,
        Guid? cardId,
        Guid? performedByUserId,
        CancellationToken cancellationToken)
        => ExecuteSingleUserFlowAsync(
            type,
            userAccountId,
            amount,
            idempotencyKey,
            description,
            apply,
            buildLedger,
            sourceAccountId,
            destinationAccountId,
            cardId,
            performedByUserId,
            manageTransaction: true,
            cancellationToken);

    private async Task<TransactionRecord> ExecuteSingleUserFlowAsync(
        TransactionType type,
        Guid userAccountId,
        decimal amount,
        string idempotencyKey,
        string description,
        Action<Account, Account, decimal> apply,
        Func<Guid, Account, Account, decimal, string, string, (LedgerEntry Debit, LedgerEntry Credit)> buildLedger,
        Guid sourceAccountId,
        Guid destinationAccountId,
        Guid? cardId,
        Guid? performedByUserId,
        bool manageTransaction,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (userAccountId == Guid.Empty)
            throw new ArgumentException("User account id is required.", nameof(userAccountId));
        if (userAccountId == SystemAccounts.ClearingAccountId)
            throw new InvalidAccountOperationException("User account cannot be the system clearing account.");
        if (amount <= 0m)
            throw new InvalidAmountException("Amount must be greater than zero.");

        if (type == TransactionType.Spend && (cardId is null || cardId == Guid.Empty))
            throw new ArgumentException("CardId is required for Spend.", nameof(cardId));

        var key = idempotencyKey.Trim();

        var existing = await FindCompletedOrRejectInFlightAsync(key, cancellationToken);
        if (existing is not null)
            return existing;

        // Card gate: after idempotency, before BeginTransaction / FOR UPDATE locks.
        if (type == TransactionType.Spend)
            await EnsureCardEligibleForSpendAsync(userAccountId, cardId!.Value, cancellationToken);

        var transactionGroupId = Guid.NewGuid();

        try
        {
            if (manageTransaction)
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var (userAccount, systemAccount) = await LockUserAndClearingAccountsAsync(
                userAccountId,
                cancellationToken);

            apply(userAccount, systemAccount, amount);

            var (debit, credit) = buildLedger(
                transactionGroupId,
                userAccount,
                systemAccount,
                amount,
                description,
                key);

            var record = TransactionRecord.Create(
                type,
                sourceAccountId,
                destinationAccountId,
                amount,
                transactionGroupId,
                key,
                cardId,
                performedByUserId,
                description);
            record.MarkCompleted();

            await _unitOfWork.LedgerEntries.AddAsync(debit, cancellationToken);
            await _unitOfWork.LedgerEntries.AddAsync(credit, cancellationToken);
            await _unitOfWork.TransactionRecords.AddAsync(record, cancellationToken);
            _unitOfWork.Accounts.Update(userAccount);
            _unitOfWork.Accounts.Update(systemAccount);

            await _failureGate.BeforeCommitAsync(cancellationToken);

            if (manageTransaction)
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return record;
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            if (manageTransaction)
            {
                await SafeRollbackAsync(cancellationToken);
                return await ResolveIdempotentRaceAsync(key, cancellationToken);
            }

            throw;
        }
        catch (ConcurrencyConflictException)
        {
            if (manageTransaction)
            {
                await SafeRollbackAsync(cancellationToken);
                await TryPersistFailedAsync(
                    type, sourceAccountId, destinationAccountId, amount, transactionGroupId, key,
                    cardId, performedByUserId, description, cancellationToken);
            }

            throw;
        }
        catch (Exception)
        {
            if (manageTransaction)
            {
                await SafeRollbackAsync(cancellationToken);
                await TryPersistFailedAsync(
                    type, sourceAccountId, destinationAccountId, amount, transactionGroupId, key,
                    cardId, performedByUserId, description, cancellationToken);
            }

            throw;
        }
    }

    private async Task EnsureCardEligibleForSpendAsync(
        Guid userAccountId,
        Guid cardId,
        CancellationToken cancellationToken)
    {
        var card = await _unitOfWork.Cards.GetByIdAsync(cardId, cancellationToken)
            ?? throw new NotFoundException(nameof(Card), cardId);

        if (card.AccountId != userAccountId)
            throw new ForbiddenException("bu kart bu hesaba ait değil");

        if (card.Status != CardStatus.Active)
        {
            throw new InvalidAccountOperationException(
                "kart bloke/süresi dolmuş, harcama yapılamaz");
        }

        if (card.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidAccountOperationException(
                "kart bloke/süresi dolmuş, harcama yapılamaz");
        }
    }

    private async Task<TransactionRecord?> FindCompletedOrRejectInFlightAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.TransactionRecords.GetByIdempotencyKeyAsync(
            idempotencyKey,
            cancellationToken);

        if (existing is null)
            return null;

        if (existing.Status == TransactionStatus.Completed)
            return existing;

        throw new IdempotencyInProgressException(idempotencyKey);
    }

    private async Task<TransactionRecord> ResolveIdempotentRaceAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var existing = await FindCompletedOrRejectInFlightAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
            return existing;

        throw new IdempotencyInProgressException(idempotencyKey);
    }

    /// <summary>
    /// Detects Postgres unique_violation (23505) without a hard Npgsql dependency
    /// in the Application layer (concurrent Idempotency-Key retries).
    /// </summary>
    private static bool IsUniqueConstraintViolation(Exception ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            var message = e.Message;
            if (message.Contains("23505", StringComparison.Ordinal)
                || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var sqlState = e.GetType().GetProperty("SqlState")?.GetValue(e) as string;
            if (sqlState == "23505")
                return true;
        }

        return false;
    }

    /// <summary>
    /// Acquires FOR UPDATE locks using <see cref="Guid.CompareTo"/> ascending order
    /// (platform-independent GUID byte order — never string/culture formatting).
    /// Lock order is independent of source/destination roles.
    /// </summary>
    private async Task<(Account Source, Account Destination)> LockTransferAccountsInIdOrderAsync(
        Guid sourceAccountId,
        Guid destinationAccountId,
        CancellationToken cancellationToken)
    {
        // Deterministic: Guid.CompareTo uses the 16-byte value comparison in .NET.
        var sourceIsFirst = sourceAccountId.CompareTo(destinationAccountId) <= 0;
        var firstId = sourceIsFirst ? sourceAccountId : destinationAccountId;
        var secondId = sourceIsFirst ? destinationAccountId : sourceAccountId;

        var first = await _unitOfWork.Accounts.GetByIdForUpdateAsync(firstId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), firstId);
        var second = await _unitOfWork.Accounts.GetByIdForUpdateAsync(secondId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), secondId);

        var source = sourceIsFirst ? first : second;
        var destination = sourceIsFirst ? second : first;
        return (source, destination);
    }

    private static void EnsureAccountActiveForTransfer(Account account)
    {
        if (account.Status != AccountStatus.Active)
        {
            throw new InvalidAccountOperationException(
                $"Account '{account.Id}' is {account.Status} and cannot participate in a transfer.");
        }
    }

    private async Task<(Account User, Account System)> LockUserAndClearingAccountsAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        var clearingId = SystemAccounts.ClearingAccountId;
        var userIsFirst = userAccountId.CompareTo(clearingId) <= 0;
        var firstId = userIsFirst ? userAccountId : clearingId;
        var secondId = userIsFirst ? clearingId : userAccountId;

        var first = await _unitOfWork.Accounts.GetByIdForUpdateAsync(firstId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), firstId);
        var second = await _unitOfWork.Accounts.GetByIdForUpdateAsync(secondId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), secondId);

        var user = userIsFirst ? first : second;
        var system = userIsFirst ? second : first;

        if (user.AccountType != AccountType.User)
            throw new InvalidAccountOperationException($"Account '{userAccountId}' is not a user account.");

        if (!system.IsSystemAccount)
            throw new InvalidAccountOperationException("System clearing account is misconfigured.");

        return (user, system);
    }

    private async Task SafeRollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        }
        catch
        {
            // Best-effort rollback; original exception is rethrown by caller.
        }
    }

    private async Task TryPersistFailedAsync(
        TransactionType type,
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        Guid transactionGroupId,
        string idempotencyKey,
        Guid? cardId,
        Guid? performedByUserId,
        string? description,
        CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _unitOfWork.TransactionRecords.GetByIdempotencyKeyAsync(
                idempotencyKey,
                cancellationToken);
            if (existing is not null)
                return;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var failed = TransactionRecord.Create(
                type,
                sourceAccountId,
                destinationAccountId,
                amount,
                transactionGroupId,
                idempotencyKey,
                cardId,
                performedByUserId,
                description);
            failed.MarkFailed();

            await _unitOfWork.TransactionRecords.AddAsync(failed, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await SafeRollbackAsync(cancellationToken);
        }
    }

    private async Task EnsureTransactionPinAsync(
        Guid accountId,
        string? pin,
        CancellationToken cancellationToken)
    {
        // Unit-test constructor uses PassThroughPasswordHasher and skips PIN gating.
        if (_passwordHasher is PassThroughPasswordHasher)
            return;

        ArgumentException.ThrowIfNullOrWhiteSpace(pin);

        // Do not track Account here: a tracked entity with a stale xmin/RowVersion
        // would break later FOR UPDATE + SaveChanges under concurrency.
        var userId = await _unitOfWork.Accounts.GetOwnerUserIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), accountId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!user.HasTransactionPin)
        {
            throw new InvalidAccountOperationException(
                "Önce işlem PIN'i belirlemelisiniz");
        }

        if (!_passwordHasher.Verify(pin, user.TransactionPinHash!))
            throw new InvalidPinException();
    }

    /// <summary>Test-only hasher so unit tests can omit PIN setup.</summary>
    private sealed class PassThroughPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => password;
        public bool Verify(string password, string passwordHash) => true;
    }
}
