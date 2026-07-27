using FluentValidation;

namespace philcare.Api.Features.Volunteers.ActivityVolunteers.AddActivityVolunteer;

public sealed class AddActivityVolunteerValidator : AbstractValidator<AddActivityVolunteerRequest>
{
    public AddActivityVolunteerValidator()
    {
        RuleFor(x => x.VolunteerId).GreaterThan(0);
        RuleFor(x => x.RoleInActivity).MaximumLength(100);
        RuleFor(x => x.AttendanceStatus).MaximumLength(50);
        RuleFor(x => x.HoursServed).GreaterThanOrEqualTo(0).When(x => x.HoursServed is not null);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}
