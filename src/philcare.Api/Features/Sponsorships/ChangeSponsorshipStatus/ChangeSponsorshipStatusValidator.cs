using FluentValidation;

namespace philcare.Api.Features.Sponsorships.ChangeSponsorshipStatus;

public sealed class ChangeSponsorshipStatusValidator : AbstractValidator<ChangeSponsorshipStatusRequest>
{
    public ChangeSponsorshipStatusValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
