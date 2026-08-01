namespace VaultLedger.Application.DTOs.BudgetCategories;

public sealed class UpdateBudgetCategoryDto
{
    public decimal? DefaultAllocatedAmount { get; set; }
    public bool? IsTransferable { get; set; }
    public bool? IsSelfRequestable { get; set; }
    public bool? IsActive { get; set; }
}
