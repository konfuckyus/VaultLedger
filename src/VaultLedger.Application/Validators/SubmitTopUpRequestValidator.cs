using FluentValidation;
using VaultLedger.Application.DTOs.Requests;
using VaultLedger.Domain.Entities;

namespace VaultLedger.Application.Validators;

public sealed class SubmitTopUpRequestValidator : AbstractValidator<SubmitTopUpRequestDto>
{
    public SubmitTopUpRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Note)
            .MaximumLength(TopUpRequest.MaxNoteLength)
            .When(x => x.Note is not null);
    }
}
