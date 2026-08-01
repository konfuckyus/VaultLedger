using FluentValidation;
using VaultLedger.Application.DTOs.Transactions;

namespace VaultLedger.Application.Validators;

public sealed class AdjustmentRequestValidator : AbstractValidator<AdjustmentRequestDto>
{
    public AdjustmentRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Direction).IsInEnum();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required for adjustments.")
            .MaximumLength(500);
    }
}
