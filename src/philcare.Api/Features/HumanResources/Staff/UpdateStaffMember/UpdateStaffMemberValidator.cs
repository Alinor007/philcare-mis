using FluentValidation;

namespace philcare.Api.Features.HumanResources.Staff.UpdateStaffMember;

public sealed class UpdateStaffMemberValidator : AbstractValidator<UpdateStaffMemberRequest>
{
    public UpdateStaffMemberValidator()
    {
        RuleFor(x => x.Position).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Department).MaximumLength(200);
        RuleFor(x => x.EmploymentType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SupervisorPersonId).GreaterThan(0).When(x => x.SupervisorPersonId is not null);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
