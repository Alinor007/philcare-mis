using FluentValidation;

namespace philcare.Api.Features.Partners.CreatePartner;

public sealed class CreatePartnerValidator : AbstractValidator<CreatePartnerRequest>
{
    public CreatePartnerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PartnerType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ContactPerson).MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Province).MaximumLength(100);
        RuleFor(x => x.Region).MaximumLength(50);
        RuleFor(x => x.MouReference).MaximumLength(100);
        RuleFor(x => x.AccreditationNotes).MaximumLength(1000);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
