using VaultLedger.Domain.Common;

namespace VaultLedger.Domain.Entities;

/// <summary>
/// Per-user grant to request an account in a category that is not self-requestable.
/// </summary>
public class CategoryEligibility : BaseEntity
{
    private CategoryEligibility()
    {
    }

    private CategoryEligibility(Guid userId, Guid categoryId, Guid grantedByAdminUserId)
    {
        UserId = userId;
        CategoryId = categoryId;
        GrantedByAdminUserId = grantedByAdminUserId;
        GrantedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid CategoryId { get; private set; }
    public BudgetCategoryDefinition Category { get; private set; } = null!;

    public Guid GrantedByAdminUserId { get; private set; }
    public User GrantedByAdminUser { get; private set; } = null!;

    public DateTime GrantedAt { get; private set; }

    public static CategoryEligibility Create(Guid userId, Guid categoryId, Guid grantedByAdminUserId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId is required.", nameof(categoryId));
        if (grantedByAdminUserId == Guid.Empty)
            throw new ArgumentException("GrantedByAdminUserId is required.", nameof(grantedByAdminUserId));

        return new CategoryEligibility(userId, categoryId, grantedByAdminUserId);
    }
}
