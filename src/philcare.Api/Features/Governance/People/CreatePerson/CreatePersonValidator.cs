using FluentValidation;

namespace philcare.Api.Features.Governance.People.CreatePerson;

public sealed class CreatePersonValidator : AbstractValidator<CreatePersonRequest>
{
    public CreatePersonValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PersonCategory).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).MaximumLength(256);
        RuleFor(x => x.ContactNumber).MaximumLength(50);
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.CivilStatus).MaximumLength(50);
        RuleFor(x => x.Barangay).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Province).MaximumLength(100);
        RuleFor(x => x.Region).MaximumLength(50);
        RuleFor(x => x.EmergencyContactName).MaximumLength(200);
        RuleFor(x => x.EmergencyContactNumber).MaximumLength(50);
        RuleFor(x => x.PhotoUrl).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
