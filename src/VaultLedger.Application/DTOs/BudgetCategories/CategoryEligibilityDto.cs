namespace VaultLedger.Application.DTOs.BudgetCategories;

public sealed class CategoryEligibilityDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid GrantedByAdminUserId { get; set; }
    public DateTime GrantedAt { get; set; }
}
