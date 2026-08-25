using FluentValidation;

namespace philcare.Api.Features.Finance.Donors.ReviewDonorKyd;

public sealed class ReviewDonorKydValidator : AbstractValidator<ReviewDonorKydRequest>
{
    public ReviewDonorKydValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
