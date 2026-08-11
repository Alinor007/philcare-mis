using FluentValidation;

namespace philcare.Api.Features.Governance.Minutes.UpdateMinutes;

public sealed class UpdateMinutesValidator : AbstractValidator<UpdateMinutesRequest>
{
    public UpdateMinutesValidator()
    {
        RuleFor(x => x.PreparedByPersonId).GreaterThan(0).When(x => x.PreparedByPersonId is not null);
        RuleFor(x => x.ApprovedByPersonId).GreaterThan(0).When(x => x.ApprovedByPersonId is not null);
        RuleFor(x => x.Summary).MaximumLength(2000);
        RuleFor(x => x.DocumentLink).MaximumLength(500);
        RuleFor(x => x.PublicationStatus).IsInEnum();
    }
}
