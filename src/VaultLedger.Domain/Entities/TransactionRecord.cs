using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Domain.Entities;

/// <summary>
/// User-facing transaction summary. Linked to immutable ledger lines via <see cref="TransactionGroupId"/>.
/// TopUp/Refund/Adjustment(Increase): Source = System Clearing, Destination = user account.
/// Spend/Adjustment(Decrease): Source = user account, Destination = System Clearing.
/// </summary>
public class TransactionRecord : BaseEntity
{
    private TransactionRecord()
    {
    }

    private TransactionRecord(
        TransactionType type,
        Guid sourceAccountId,
        Guid? destinationAccountId,
        decimal amount,
        Guid transactionGroupId,
        string idempotencyKey)
    {
        Type = type;
        SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId;
        Amount = amount;
        Status = TransactionStatus.Pending;
        TransactionGroupId = transactionGroupId;
        IdempotencyKey = idempotencyKey;
    }

    public TransactionType Type { get; private set; }
    public Guid SourceAccountId { get; private set; }
    public Account SourceAccount { get; private set; } = null!;
    public Guid? DestinationAccountId { get; private set; }
    public Account? DestinationAccount { get; private set; }
    public Guid? CardId { get; private set; }
    public Card? Card { get; private set; }

    /// <summary>Admin (or actor) who performed the operation — set for TopUp/Adjustment.</summary>
    public Guid? PerformedByUserId { get; private set; }
    public User? PerformedByUser { get; private set; }

    public decimal Amount { get; private set; }
    public TransactionStatus Status { get; private set; }
    public Guid TransactionGroupId { get; private set; }

    /// <summary>Optional narrative; required and persisted for Adjustment.</summary>
    public string? Description { get; private set; }

    /// <summary>Client Idempotency-Key for request-level deduplication (Adım 5).</summary>
    public string IdempotencyKey { get; private set; } = null!;

    public static TransactionRecord Create(
        TransactionType type,
        Guid sourceAccountId,
        Guid? destinationAccountId,
        decimal amount,
        Guid transactionGroupId,
        string idempotencyKey,
        Guid? cardId = null,
        Guid? performedByUserId = null,
        string? description = null)
    {
        if (sourceAccountId == Guid.Empty)
            throw new ArgumentException("SourceAccountId is required.", nameof(sourceAccountId));

        if (transactionGroupId == Guid.Empty)
            throw new ArgumentException("TransactionGroupId is required.", nameof(transactionGroupId));

        if (amount <= 0m)
            throw new InvalidAmountException("Transaction amount must be greater than zero.");

        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        switch (type)
        {
            case TransactionType.Spend:
                if (cardId is null || cardId == Guid.Empty)
                    throw new ArgumentException("CardId is required for Spend.", nameof(cardId));
                break;

            case TransactionType.Transfer:
                EnsureDistinctAccounts(sourceAccountId, destinationAccountId, "transfers");
                EnsureNoCard(type, cardId);
                break;

            case TransactionType.TopUp:
                if (performedByUserId is null || performedByUserId == Guid.Empty)
                {
                    throw new ArgumentException(
                        "PerformedByUserId is required for TopUp.",
                        nameof(performedByUserId));
                }

                if (sourceAccountId != SystemAccounts.ClearingAccountId)
                {
                    throw new InvalidAccountOperationException(
                        $"{type} must use the system clearing account as SourceAccountId.");
                }

                EnsureDistinctAccounts(sourceAccountId, destinationAccountId, type.ToString());
                EnsureNoCard(type, cardId);
                break;

            case TransactionType.Refund:
                if (sourceAccountId != SystemAccounts.ClearingAccountId)
                {
                    throw new InvalidAccountOperationException(
                        $"{type} must use the system clearing account as SourceAccountId.");
                }

                EnsureDistinctAccounts(sourceAccountId, destinationAccountId, type.ToString());
                EnsureNoCard(type, cardId);
                break;

            case TransactionType.Adjustment:
                if (performedByUserId is null || performedByUserId == Guid.Empty)
                {
                    throw new ArgumentException(
                        "PerformedByUserId is required for Adjustment.",
                        nameof(performedByUserId));
                }

                if (trimmedDescription is null)
                {
                    throw new ArgumentException(
                        "Reason/description is required for Adjustment.",
                        nameof(description));
                }

                EnsureDistinctAccounts(sourceAccountId, destinationAccountId, type.ToString());
                EnsureNoCard(type, cardId);

                var clearing = SystemAccounts.ClearingAccountId;
                var touchesClearing =
                    sourceAccountId == clearing || destinationAccountId == clearing;
                if (!touchesClearing)
                {
                    throw new InvalidAccountOperationException(
                        "Adjustment must involve the system clearing account.");
                }

                break;

            default:
                EnsureNoCard(type, cardId);
                break;
        }

        return new TransactionRecord(
            type,
            sourceAccountId,
            destinationAccountId,
            amount,
            transactionGroupId,
            idempotencyKey.Trim())
        {
            CardId = cardId,
            PerformedByUserId = performedByUserId,
            Description = trimmedDescription
        };
    }

    private static void EnsureNoCard(TransactionType type, Guid? cardId)
    {
        if (cardId is not null && cardId != Guid.Empty)
        {
            throw new InvalidAccountOperationException(
                $"{type} must not include a CardId.");
        }
    }

    public void MarkCompleted() => Status = TransactionStatus.Completed;

    public void MarkFailed() => Status = TransactionStatus.Failed;

    public void MarkRolledBack() => Status = TransactionStatus.RolledBack;

    private static void EnsureDistinctAccounts(
        Guid sourceAccountId,
        Guid? destinationAccountId,
        string operationName)
    {
        if (destinationAccountId is null || destinationAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                $"DestinationAccountId is required for {operationName}.",
                nameof(destinationAccountId));
        }

        if (destinationAccountId == sourceAccountId)
        {
            throw new InvalidAccountOperationException(
                $"Source and destination accounts must differ for {operationName}.");
        }
    }
}
