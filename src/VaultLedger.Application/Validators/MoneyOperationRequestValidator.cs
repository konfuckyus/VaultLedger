using FluentValidation;
using VaultLedger.Application.DTOs.Transactions;

namespace VaultLedger.Application.Validators;

public sealed class MoneyOperationRequestValidator : AbstractValidator<MoneyOperationRequestDto>
{
    public MoneyOperationRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
