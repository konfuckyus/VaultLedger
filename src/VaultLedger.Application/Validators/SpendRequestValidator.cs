using FluentValidation;
using VaultLedger.Application.DTOs.Transactions;

namespace VaultLedger.Application.Validators;

public sealed class SpendRequestValidator : AbstractValidator<SpendRequestDto>
{
    public SpendRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.Pin)
            .NotEmpty()
            .Matches(@"^\d{4}$")
            .WithMessage("PIN must be exactly 4 digits.");
    }
}
