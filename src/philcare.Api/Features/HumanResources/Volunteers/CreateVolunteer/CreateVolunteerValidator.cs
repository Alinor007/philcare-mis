using FluentValidation;

namespace philcare.Api.Features.HumanResources.Volunteers.CreateVolunteer;

public sealed class CreateVolunteerValidator : AbstractValidator<CreateVolunteerRequest>
{
    public CreateVolunteerValidator()
    {
        RuleFor(x => x.PersonId).GreaterThan(0);
        RuleFor(x => x.Skills).MaximumLength(500);
        RuleFor(x => x.AvailabilityDays).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
