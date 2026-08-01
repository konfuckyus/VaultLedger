using FluentValidation;
using VaultLedger.Application.DTOs.Accounts;

namespace VaultLedger.Application.Validators;

public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequestDto>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Currency).MaximumLength(3).When(x => x.Currency is not null);
    }
}
