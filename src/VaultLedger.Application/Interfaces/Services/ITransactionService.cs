using VaultLedger.Domain.Entities;
using VaultLedger.Domain.Enums;

namespace VaultLedger.Application.Interfaces.Services;

public interface ITransactionService
{
    Task<TransactionRecord> SpendAsync(
        Guid userAccountId,
        Guid cardId,
        decimal amount,
        string idempotencyKey,
        string? description = null,
        string? pin = null,
        CancellationToken cancellationToken = default);

    Task<TransactionRecord> TopUpAsync(
        Guid targetAccountId,
        decimal amount,
        string idempotencyKey,
        string? description,
        Guid performedByAdminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Same as <see cref="TopUpAsync"/> but assumes the caller already opened a unit-of-work transaction.
    /// </summary>
    Task<TransactionRecord> TopUpInCurrentTransactionAsync(
        Guid targetAccountId,
        decimal amount,
        string idempotencyKey,
        string? description,
        Guid performedByAdminUserId,
        CancellationToken cancellationToken = default);

    Task<TransactionRecord> RefundAsync(
        Guid userAccountId,
        decimal amount,
        string idempotencyKey,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task<TransactionRecord> TransferAsync(
        Guid sourceAccountId,
        Guid destinationAccountId,
        decimal amount,
        string idempotencyKey,
        string? description = null,
        string? pin = null,
        CancellationToken cancellationToken = default);

    Task<TransactionRecord> AdjustmentAsync(
        Guid targetAccountId,
        decimal amount,
        AdjustmentDirection direction,
        string reason,
        string idempotencyKey,
        Guid performedByAdminUserId,
        CancellationToken cancellationToken = default);
}
