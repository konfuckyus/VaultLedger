using FluentValidation;
using VaultLedger.Application.DTOs.Requests;

namespace VaultLedger.Application.Validators;

public sealed class SubmitAccountRequestValidator : AbstractValidator<SubmitAccountRequestDto>
{
    public SubmitAccountRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
