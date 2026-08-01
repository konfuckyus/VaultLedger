using FluentValidation;
using VaultLedger.Application.DTOs.BudgetCategories;

namespace VaultLedger.Application.Validators;

public sealed class UpdateBudgetCategoryValidator : AbstractValidator<UpdateBudgetCategoryDto>
{
    public UpdateBudgetCategoryValidator()
    {
        RuleFor(x => x.DefaultAllocatedAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DefaultAllocatedAmount.HasValue);
    }
}
