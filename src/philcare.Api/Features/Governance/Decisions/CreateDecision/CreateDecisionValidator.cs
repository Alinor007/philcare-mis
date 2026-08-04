using FluentValidation;

namespace philcare.Api.Features.Governance.Decisions.CreateDecision;

public sealed class CreateDecisionValidator : AbstractValidator<CreateDecisionRequest>
{
    public CreateDecisionValidator()
    {
        RuleFor(x => x.DecisionText).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ActionPoints).MaximumLength(2000);
        RuleFor(x => x.ResponsiblePersonId).GreaterThan(0).When(x => x.ResponsiblePersonId is not null);
        RuleFor(x => x.DecisionStatus).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
