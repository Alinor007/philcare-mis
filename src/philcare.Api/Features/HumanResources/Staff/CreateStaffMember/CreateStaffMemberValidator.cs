using FluentValidation;

namespace philcare.Api.Features.HumanResources.Staff.CreateStaffMember;

public sealed class CreateStaffMemberValidator : AbstractValidator<CreateStaffMemberRequest>
{
    public CreateStaffMemberValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Department).MaximumLength(200);
        RuleFor(x => x.EmploymentType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
