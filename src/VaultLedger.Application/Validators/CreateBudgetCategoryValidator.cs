using FluentValidation;
using VaultLedger.Application.DTOs.BudgetCategories;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Validators;

public sealed class CreateBudgetCategoryValidator : AbstractValidator<CreateBudgetCategoryDto>
{
    public CreateBudgetCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(BudgetCategoryDefinition.MaxNameLength);
        RuleFor(x => x.DefaultAllocatedAmount)
            .GreaterThanOrEqualTo(0);
    }
}
