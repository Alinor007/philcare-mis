using FluentValidation;

namespace philcare.Api.Features.Programs.Activities.ChangeActivityStatus;

public sealed class ChangeActivityStatusValidator : AbstractValidator<ChangeActivityStatusRequest>
{
    public ChangeActivityStatusValidator()
    {
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ActualBeneficiaries).GreaterThanOrEqualTo(0).When(x => x.ActualBeneficiaries is not null);
    }
}
