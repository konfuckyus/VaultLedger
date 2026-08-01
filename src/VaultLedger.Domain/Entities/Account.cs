using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Domain.Entities;

public class Account : BaseEntity
{
    public const string DefaultCurrency = "TRY";
    public const int AccountNumberLength = 10;

    private readonly List<Card> _cards = [];
    private readonly List<LedgerEntry> _ledgerEntries = [];

    private Account()
    {
    }

    private Account(Guid userId, string accountNumber, string currency, AccountType accountType)
    {
        UserId = userId;
        AccountNumber = accountNumber;
        Balance = 0m;
        Currency = currency;
        AccountType = accountType;
        Status = AccountStatus.Active;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    /// <summary>Unique 10-digit account number assigned when the account is created.</summary>
    public string AccountNumber { get; private set; } = null!;

    /// <summary>
    /// Cached balance. Source of truth is the ledger; this must stay consistent with LedgerEntry totals.
    /// </summary>
    public decimal Balance { get; private set; }

    public string Currency { get; private set; } = DefaultCurrency;
    public AccountType AccountType { get; private set; }
    public AccountStatus Status { get; private set; }

    /// <summary>Budget category snapshot at account creation (null for system clearing).</summary>
    public Guid? CategoryId { get; private set; }
    public BudgetCategoryDefinition? Category { get; private set; }

    /// <summary>
    /// Snapshot of category.IsTransferable at create time. Non-transferable accounts cannot send transfers.
    /// </summary>
    public bool IsTransferable { get; private set; } = true;

    /// <summary>
    /// PostgreSQL xmin concurrency token (EF Core <c>IsRowVersion</c>).
    /// Prefer uint/xmin over SQL Server-style byte[] timestamp on Npgsql.
    /// </summary>
    public uint RowVersion { get; private set; }

    public bool IsSystemAccount => AccountType == AccountType.System;

    public IReadOnlyCollection<Card> Cards => _cards.AsReadOnly();
    public IReadOnlyCollection<LedgerEntry> LedgerEntries => _ledgerEntries.AsReadOnly();

    public static Account Create(
        Guid userId,
        string accountNumber,
        Guid categoryId,
        bool isTransferable,
        string currency = DefaultCurrency)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId is required.", nameof(categoryId));

        EnsureValidAccountNumber(accountNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        return new Account(
            userId,
            accountNumber,
            currency.Trim().ToUpperInvariant(),
            AccountType.User)
        {
            CategoryId = categoryId,
            IsTransferable = isTransferable
        };
    }

    /// <summary>
    /// Creates the house/clearing account. Prefer seeded <see cref="SystemAccounts.ClearingAccountId"/>.
    /// </summary>
    public static Account CreateSystemClearing(Guid systemUserId, string currency = DefaultCurrency)
    {
        if (systemUserId == Guid.Empty)
            throw new ArgumentException("System user id is required.", nameof(systemUserId));

        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        return new Account(
            systemUserId,
            SystemAccounts.ClearingAccountNumber,
            currency.Trim().ToUpperInvariant(),
            AccountType.System)
        {
            Id = SystemAccounts.ClearingAccountId,
            CategoryId = null,
            IsTransferable = false
        };
    }

    private static void EnsureValidAccountNumber(string accountNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber);

        if (accountNumber.Length != AccountNumberLength || !accountNumber.All(char.IsDigit))
            throw new ArgumentException(
                $"AccountNumber must be exactly {AccountNumberLength} digits.",
                nameof(accountNumber));
    }

    public void Debit(decimal amount)
    {
        EnsureActive();
        EnsurePositiveAmount(amount);

        // User accounts cannot overdraw; system clearing may go negative as TopUp/Refund counterparty.
        if (AccountType == AccountType.User && Balance < amount)
            throw new InsufficientBalanceException(Id, amount, Balance);

        var balanceAfter = Balance - amount;

        if (AccountType == AccountType.User && balanceAfter < 0m)
            throw new InvalidAccountOperationException($"User account '{Id}' balance cannot be negative.");

        Balance = balanceAfter;
    }

    public void Credit(decimal amount)
    {
        EnsureActive();
        EnsurePositiveAmount(amount);

        var balanceAfter = Balance + amount;

        // Defensive: user balances must never go negative (Credit normally only increases).
        if (AccountType == AccountType.User && balanceAfter < 0m)
            throw new InvalidAccountOperationException($"User account '{Id}' balance cannot be negative.");

        Balance = balanceAfter;
    }

    public void Suspend()
    {
        EnsureNotSystem("suspend");
        Status = AccountStatus.Suspended;
    }

    public void Close()
    {
        EnsureNotSystem("close");

        if (Balance != 0m)
            throw new InvalidAccountOperationException("An account with a non-zero balance cannot be closed.");

        Status = AccountStatus.Closed;
    }

    public void Reactivate()
    {
        EnsureNotSystem("reactivate");

        if (Status == AccountStatus.Closed)
            throw new InvalidAccountOperationException("A closed account cannot be reactivated.");

        Status = AccountStatus.Active;
    }

    private void EnsureNotSystem(string operation)
    {
        if (IsSystemAccount)
            throw new InvalidAccountOperationException($"Cannot {operation} the system clearing account.");
    }

    private void EnsureActive()
    {
        if (Status != AccountStatus.Active)
            throw new InvalidAccountOperationException($"Account '{Id}' is {Status} and cannot be modified.");
    }

    private static void EnsurePositiveAmount(decimal amount)
    {
        if (amount <= 0m)
            throw new InvalidAmountException("Amount must be greater than zero.");
    }
}
