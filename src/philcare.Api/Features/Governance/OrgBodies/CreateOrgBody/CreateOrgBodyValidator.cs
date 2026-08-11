using FluentValidation;

namespace philcare.Api.Features.Governance.OrgBodies.CreateOrgBody;

public sealed class CreateOrgBodyValidator : AbstractValidator<CreateOrgBodyRequest>
{
    public CreateOrgBodyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BodyType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ParentBodyId).GreaterThan(0).When(x => x.ParentBodyId is not null);
        RuleFor(x => x.QuorumRule).MaximumLength(200);
        RuleFor(x => x.DecisionThreshold).MaximumLength(200);
        RuleFor(x => x.MeetingFrequency).MaximumLength(100);
        RuleFor(x => x.PolicyBasis).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
