using FluentValidation;

namespace philcare.Api.Features.Programs.Activities.UpdateActivity;

public sealed class UpdateActivityValidator : AbstractValidator<UpdateActivityRequest>
{
    public UpdateActivityValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ActivityCategory).MaximumLength(100);
        RuleFor(x => x.ActivityType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.TargetGroup).MaximumLength(200);
        RuleFor(x => x.Barangay).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Province).MaximumLength(100);
        RuleFor(x => x.Region).MaximumLength(50);
        RuleFor(x => x.Budget).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ImplementingPartner).MaximumLength(200);
        RuleFor(x => x.ImplementingPartnerId).GreaterThan(0).When(x => x.ImplementingPartnerId is not null);
        RuleFor(x => x.ResponsibleDepartment).MaximumLength(200);
        RuleFor(x => x.SdgAlignment).MaximumLength(100);
        RuleFor(x => x.ImplementationStatus).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SafeguardingRisk).MaximumLength(100);
        RuleFor(x => x.EvidenceLink).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
