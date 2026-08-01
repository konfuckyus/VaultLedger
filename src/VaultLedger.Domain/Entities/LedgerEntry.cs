using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Domain.Entities;

/// <summary>
/// Immutable ledger line. Balance is never the sole source of truth — entries are.
/// For a single business operation, debit and credit rows share the same <see cref="TransactionGroupId"/>.
/// </summary>
public class LedgerEntry : BaseEntity
{
    private LedgerEntry()
    {
    }

    private LedgerEntry(
        Guid transactionGroupId,
        Guid accountId,
        EntryType entryType,
        decimal amount,
        decimal balanceAfter,
        string description,
        string idempotencyKey)
    {
        TransactionGroupId = transactionGroupId;
        AccountId = accountId;
        EntryType = entryType;
        Amount = amount;
        BalanceAfter = balanceAfter;
        Description = description;
        IdempotencyKey = idempotencyKey;
    }

    public Guid TransactionGroupId { get; private set; }
    public Guid AccountId { get; private set; }
    public Account Account { get; private set; } = null!;
    public EntryType EntryType { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string Description { get; private set; } = null!;

    /// <summary>
    /// Must be unique per ledger row. For double-entry pairs, use a distinct key per side
    /// (e.g. "{clientKey}:debit" / "{clientKey}:credit").
    /// </summary>
    public string IdempotencyKey { get; private set; } = null!;

    public static LedgerEntry CreateDebit(
        Guid transactionGroupId,
        Guid accountId,
        decimal amount,
        decimal balanceAfter,
        string description,
        string idempotencyKey)
        => Create(transactionGroupId, accountId, EntryType.Debit, amount, balanceAfter, description, idempotencyKey);

    public static LedgerEntry CreateCredit(
        Guid transactionGroupId,
        Guid accountId,
        decimal amount,
        decimal balanceAfter,
        string description,
        string idempotencyKey)
        => Create(transactionGroupId, accountId, EntryType.Credit, amount, balanceAfter, description, idempotencyKey);

    private static LedgerEntry Create(
        Guid transactionGroupId,
        Guid accountId,
        EntryType entryType,
        decimal amount,
        decimal balanceAfter,
        string description,
        string idempotencyKey)
    {
        if (transactionGroupId == Guid.Empty)
            throw new ArgumentException("TransactionGroupId is required.", nameof(transactionGroupId));

        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId is required.", nameof(accountId));

        if (amount <= 0m)
            throw new InvalidAmountException("Ledger amount must be greater than zero.");

        // System clearing BalanceAfter may be negative (TopUp/Refund counterparty).
        // User accounts are guarded in Account.Debit/Credit.

        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        return new LedgerEntry(
            transactionGroupId,
            accountId,
            entryType,
            amount,
            balanceAfter,
            description.Trim(),
            idempotencyKey.Trim());
    }
}
