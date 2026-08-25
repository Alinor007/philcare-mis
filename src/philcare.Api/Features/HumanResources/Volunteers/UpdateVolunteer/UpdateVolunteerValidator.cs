using FluentValidation;

namespace philcare.Api.Features.HumanResources.Volunteers.UpdateVolunteer;

public sealed class UpdateVolunteerValidator : AbstractValidator<UpdateVolunteerRequest>
{
    public UpdateVolunteerValidator()
    {
        RuleFor(x => x.Skills).MaximumLength(500);
        RuleFor(x => x.AvailabilityDays).MaximumLength(200);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
