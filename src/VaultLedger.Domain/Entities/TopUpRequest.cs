using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Domain.Entities;

/// <summary>
/// User request for an admin-approved balance top-up. Money movement happens in Application on approval.
/// </summary>
public class TopUpRequest : BaseEntity
{
    public const int MaxNoteLength = 500;

    private TopUpRequest()
    {
    }

    private TopUpRequest(Guid userId, Guid accountId, decimal amount, string? note)
    {
        UserId = userId;
        AccountId = accountId;
        Amount = amount;
        Note = note;
        Status = RequestStatus.Pending;
        RequestedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid AccountId { get; private set; }
    public Account Account { get; private set; } = null!;

    public decimal Amount { get; private set; }

    /// <summary>Optional user-provided reason for the top-up request.</summary>
    public string? Note { get; private set; }

    public RequestStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public User? ReviewedByUser { get; private set; }
    public string? RejectionReason { get; private set; }

    public Guid? ResultingTransactionRecordId { get; private set; }
    public TransactionRecord? ResultingTransactionRecord { get; private set; }

    public static TopUpRequest Create(
        Guid userId,
        Guid accountId,
        decimal amount,
        bool hasPendingForAccount,
        string? note = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId is required.", nameof(accountId));

        if (amount <= 0m)
            throw new InvalidAmountException("Top-up request amount must be greater than zero.");

        if (hasPendingForAccount)
        {
            throw new InvalidRequestOperationException(
                "User already has a pending top-up request for this account.");
        }

        string? normalizedNote = null;
        if (!string.IsNullOrWhiteSpace(note))
        {
            normalizedNote = note.Trim();
            if (normalizedNote.Length > MaxNoteLength)
            {
                throw new ArgumentException(
                    $"Note cannot exceed {MaxNoteLength} characters.",
                    nameof(note));
            }
        }

        return new TopUpRequest(userId, accountId, amount, normalizedNote);
    }

    public void Approve(Guid reviewedByUserId, Guid resultingTransactionRecordId)
    {
        EnsurePending();

        if (reviewedByUserId == Guid.Empty)
            throw new ArgumentException("ReviewedByUserId is required.", nameof(reviewedByUserId));

        if (resultingTransactionRecordId == Guid.Empty)
        {
            throw new ArgumentException(
                "ResultingTransactionRecordId is required.",
                nameof(resultingTransactionRecordId));
        }

        Status = RequestStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewedByUserId;
        ResultingTransactionRecordId = resultingTransactionRecordId;
    }

    public void Reject(Guid reviewedByUserId, string reason)
    {
        EnsurePending();

        if (reviewedByUserId == Guid.Empty)
            throw new ArgumentException("ReviewedByUserId is required.", nameof(reviewedByUserId));

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = RequestStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewedByUserId;
        RejectionReason = reason.Trim();
    }

    private void EnsurePending()
    {
        if (Status != RequestStatus.Pending)
        {
            throw new InvalidRequestOperationException(
                $"Top-up request '{Id}' is {Status} and cannot be reviewed.");
        }
    }
}
