using VaultLedger.Domain.Common;
using VaultLedger.Domain.Enums;
using VaultLedger.Domain.Exceptions;

namespace VaultLedger.Domain.Entities;

/// <summary>
/// User request to open an account for a budget category.
/// Account creation happens in Application on approval.
/// </summary>
public class AccountRequest : BaseEntity
{
    private AccountRequest()
    {
    }

    private AccountRequest(Guid userId, Guid categoryId)
    {
        UserId = userId;
        CategoryId = categoryId;
        Status = RequestStatus.Pending;
        RequestedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid CategoryId { get; private set; }
    public BudgetCategoryDefinition Category { get; private set; } = null!;

    public RequestStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public User? ReviewedByUser { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? ResultingAccountId { get; private set; }
    public Account? ResultingAccount { get; private set; }

    /// <summary>
    /// Creates a pending account request for a category.
    /// Domain rule: at most one pending request per user+category.
    /// </summary>
    public static AccountRequest Create(Guid userId, Guid categoryId, bool hasPendingForCategory)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId is required.", nameof(categoryId));

        if (hasPendingForCategory)
        {
            throw new InvalidRequestOperationException(
                "User already has a pending account request for this category.");
        }

        return new AccountRequest(userId, categoryId);
    }

    public void Approve(Guid reviewedByUserId, Guid resultingAccountId)
    {
        EnsurePending();

        if (reviewedByUserId == Guid.Empty)
            throw new ArgumentException("ReviewedByUserId is required.", nameof(reviewedByUserId));

        if (resultingAccountId == Guid.Empty)
            throw new ArgumentException("ResultingAccountId is required.", nameof(resultingAccountId));

        Status = RequestStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewedByUserId;
        ResultingAccountId = resultingAccountId;
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
                $"Account request '{Id}' is {Status} and cannot be reviewed.");
    }
}
