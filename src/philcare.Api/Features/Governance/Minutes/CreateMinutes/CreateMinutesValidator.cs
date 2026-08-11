using FluentValidation;

namespace philcare.Api.Features.Governance.Minutes.CreateMinutes;

public sealed class CreateMinutesValidator : AbstractValidator<CreateMinutesRequest>
{
    public CreateMinutesValidator()
    {
        RuleFor(x => x.PreparedByPersonId).GreaterThan(0).When(x => x.PreparedByPersonId is not null);
        RuleFor(x => x.ApprovedByPersonId).GreaterThan(0).When(x => x.ApprovedByPersonId is not null);
        RuleFor(x => x.Summary).MaximumLength(2000);
        RuleFor(x => x.DocumentLink).MaximumLength(500);
    }
}
