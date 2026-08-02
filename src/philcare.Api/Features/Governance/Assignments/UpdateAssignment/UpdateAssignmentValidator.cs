using FluentValidation;

namespace philcare.Api.Features.Governance.Assignments.UpdateAssignment;

public sealed class UpdateAssignmentValidator : AbstractValidator<UpdateAssignmentRequest>
{
    public UpdateAssignmentValidator()
    {
        RuleFor(x => x.PositionTitle).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
