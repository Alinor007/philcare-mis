using FluentValidation;

namespace philcare.Api.Features.Volunteers.UpdateVolunteer;

public sealed class UpdateVolunteerValidator : AbstractValidator<UpdateVolunteerRequest>
{
    public UpdateVolunteerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Email).MaximumLength(256);
        RuleFor(x => x.Barangay).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Province).MaximumLength(100);
        RuleFor(x => x.Region).MaximumLength(50);
        RuleFor(x => x.Skills).MaximumLength(500);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
