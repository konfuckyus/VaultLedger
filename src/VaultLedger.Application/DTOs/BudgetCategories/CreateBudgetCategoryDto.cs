namespace VaultLedger.Application.DTOs.BudgetCategories;

public sealed class CreateBudgetCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public decimal DefaultAllocatedAmount { get; set; }
    public bool IsTransferable { get; set; }
    public bool IsSelfRequestable { get; set; } = true;
}
