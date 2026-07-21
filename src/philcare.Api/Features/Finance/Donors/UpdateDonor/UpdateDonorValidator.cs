using FluentValidation;

namespace philcare.Api.Features.Finance.Donors.UpdateDonor;

public sealed class UpdateDonorValidator : AbstractValidator<UpdateDonorRequest>
{
    public UpdateDonorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.KydStatus).IsInEnum();
        RuleFor(x => x.RiskRating).IsInEnum();
    }
}
