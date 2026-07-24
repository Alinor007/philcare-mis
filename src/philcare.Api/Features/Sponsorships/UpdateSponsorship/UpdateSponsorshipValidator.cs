using FluentValidation;

namespace philcare.Api.Features.Sponsorships.UpdateSponsorship;

public sealed class UpdateSponsorshipValidator : AbstractValidator<UpdateSponsorshipRequest>
{
    public UpdateSponsorshipValidator()
    {
        RuleFor(x => x.SponsorshipType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MonthlyAmountPhp).GreaterThan(0);
        RuleFor(x => x.CaseWorker).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
