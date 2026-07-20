using FluentValidation;

namespace philcare.Api.Features.Finance.Donors.CreateDonor;

public sealed class CreateDonorValidator : AbstractValidator<CreateDonorRequest>
{
    public CreateDonorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(500);
    }
}
