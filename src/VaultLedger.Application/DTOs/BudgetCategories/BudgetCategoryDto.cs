namespace VaultLedger.Application.DTOs.BudgetCategories;

public sealed class BudgetCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DefaultAllocatedAmount { get; set; }
    public bool IsTransferable { get; set; }
    public bool IsSelfRequestable { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}
