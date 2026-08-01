using FluentValidation;
using VaultLedger.Application.DTOs.Auth;

namespace VaultLedger.Application.Validators;

public sealed class SetTransactionPinValidator : AbstractValidator<SetTransactionPinDto>
{
    public SetTransactionPinValidator()
    {
        RuleFor(x => x.Pin)
            .NotEmpty()
            .Matches(@"^\d{4}$")
            .WithMessage("PIN must be exactly 4 digits.");

        RuleFor(x => x.OldPin)
            .Matches(@"^\d{4}$")
            .When(x => !string.IsNullOrWhiteSpace(x.OldPin))
            .WithMessage("Old PIN must be exactly 4 digits.");
    }
}
