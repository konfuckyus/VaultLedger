using FluentValidation;
using VaultLedger.Application.DTOs.Transactions;

namespace VaultLedger.Application.Validators;

public sealed class TransferRequestValidator : AbstractValidator<TransferRequestDto>
{
    public TransferRequestValidator()
    {
        RuleFor(x => x.SourceAccountId).NotEmpty();
        RuleFor(x => x.DestinationAccountId).NotEmpty()
            .NotEqual(x => x.SourceAccountId)
            .WithMessage("Source and destination accounts must differ.");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.Pin)
            .NotEmpty()
            .Matches(@"^\d{4}$")
            .WithMessage("PIN must be exactly 4 digits.");
    }
}
