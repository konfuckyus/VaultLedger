using FluentValidation;
using VaultLedger.Application.DTOs.BudgetCategories;

namespace VaultLedger.Application.Validators;

public sealed class GrantCategoryEligibilityValidator : AbstractValidator<GrantCategoryEligibilityDto>
{
    public GrantCategoryEligibilityValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
