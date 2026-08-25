using FluentValidation;

namespace philcare.Api.Features.HumanResources.Staff.CreateStaffMember;

public sealed class CreateStaffMemberValidator : AbstractValidator<CreateStaffMemberRequest>
{
    public CreateStaffMemberValidator()
    {
        RuleFor(x => x.PersonId).GreaterThan(0);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Department).MaximumLength(200);
        RuleFor(x => x.EmploymentType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SupervisorPersonId).GreaterThan(0).When(x => x.SupervisorPersonId is not null);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
