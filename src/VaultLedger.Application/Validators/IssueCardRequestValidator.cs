using FluentValidation;
using VaultLedger.Application.DTOs.Cards;

namespace VaultLedger.Application.Validators;

public sealed class IssueCardRequestValidator : AbstractValidator<IssueCardRequestDto>
{
    public IssueCardRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.CardNumber)
            .NotEmpty()
            .Matches(@"^\d{13,19}$")
            .WithMessage("CardNumber must be 13-19 digits.");
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.Label).MaximumLength(64).When(x => x.Label is not null);
    }
}
