using FluentValidation;

namespace philcare.Api.Features.Governance.Assignments.CreateAssignment;

public sealed class CreateAssignmentValidator : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentValidator()
    {
        RuleFor(x => x.PersonId).GreaterThan(0);
        RuleFor(x => x.OrgBodyId).GreaterThan(0);
        RuleFor(x => x.GovernanceRoleId).GreaterThan(0);
        RuleFor(x => x.PositionTitle).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
