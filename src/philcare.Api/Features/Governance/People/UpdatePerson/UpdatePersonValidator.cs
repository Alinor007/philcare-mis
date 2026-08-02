using FluentValidation;

namespace philcare.Api.Features.Governance.People.UpdatePerson;

public sealed class UpdatePersonValidator : AbstractValidator<UpdatePersonRequest>
{
    public UpdatePersonValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PersonCategory).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).MaximumLength(256);
        RuleFor(x => x.ContactNumber).MaximumLength(50);
        RuleFor(x => x.VolunteerId).GreaterThan(0).When(x => x.VolunteerId is not null);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
