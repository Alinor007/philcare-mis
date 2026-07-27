using FluentValidation;

namespace philcare.Api.Features.Zakat.DecideZakatEligibility;

public sealed class DecideZakatEligibilityValidator : AbstractValidator<DecideZakatEligibilityRequest>
{
    public DecideZakatEligibilityValidator()
    {
        RuleFor(x => x.DecidedBy).MaximumLength(100);
        RuleFor(x => x.RejectionReason).MaximumLength(1000);
        RuleFor(x => x.RejectionReason).NotEmpty().When(x => !x.Approve)
            .WithMessage("A rejection reason is required when rejecting a case.");
    }
}
