namespace VaultLedger.Application.DTOs.BudgetCategories;

public sealed class GrantCategoryEligibilityDto
{
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
}
