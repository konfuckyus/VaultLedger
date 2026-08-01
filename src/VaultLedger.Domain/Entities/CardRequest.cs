using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Domain.Entities;

/// <summary>
/// User request to issue a card for an existing account. Card issuance happens in Application
/// on approval; this entity only tracks the approval lifecycle.
/// Ownership of the target account (approved account exists) is enforced in Application.
/// Multiple cards per account are allowed; only one Pending request per user+account at a time.
/// </summary>
public class CardRequest : BaseEntity
{
    private CardRequest()
    {
    }

    private CardRequest(Guid userId, Guid accountId, string? label)
    {
        UserId = userId;
        AccountId = accountId;
        Label = NormalizeLabel(label);
        Status = RequestStatus.Pending;
        RequestedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid AccountId { get; private set; }
    public Account Account { get; private set; } = null!;

    /// <summary>Optional label carried onto the issued Card on approval.</summary>
    public string? Label { get; private set; }

    public RequestStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public User? ReviewedByUser { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? ResultingCardId { get; private set; }
    public Card? ResultingCard { get; private set; }

    /// <summary>
    /// Creates a pending card request.
    /// Domain rule: a user may not have more than one pending card request for the same account.
    /// Already-approved cards on the account do not block a new request.
    /// </summary>
    public static CardRequest Create(
        Guid userId,
        Guid accountId,
        bool hasPendingRequestForAccount,
        string? label = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId is required.", nameof(accountId));

        if (hasPendingRequestForAccount)
            throw new InvalidRequestOperationException(
                "User already has a pending card request for this account.");

        return new CardRequest(userId, accountId, label);
    }

    public void Approve(Guid reviewedByUserId, Guid resultingCardId)
    {
        EnsurePending();

        if (reviewedByUserId == Guid.Empty)
            throw new ArgumentException("ReviewedByUserId is required.", nameof(reviewedByUserId));

        if (resultingCardId == Guid.Empty)
            throw new ArgumentException("ResultingCardId is required.", nameof(resultingCardId));

        Status = RequestStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewedByUserId;
        ResultingCardId = resultingCardId;
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
            throw new InvalidRequestOperationException(
                $"Card request '{Id}' is {Status} and cannot be reviewed.");
    }

    private static string? NormalizeLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return null;

        var trimmed = label.Trim();
        if (trimmed.Length > Card.MaxLabelLength)
        {
            throw new ArgumentException(
                $"Label must be at most {Card.MaxLabelLength} characters.",
                nameof(label));
        }

        return trimmed;
    }
}
