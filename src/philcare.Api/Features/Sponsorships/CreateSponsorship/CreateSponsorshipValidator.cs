using FluentValidation;

namespace philcare.Api.Features.Sponsorships.CreateSponsorship;

public sealed class CreateSponsorshipValidator : AbstractValidator<CreateSponsorshipRequest>
{
    public CreateSponsorshipValidator()
    {
        RuleFor(x => x.DonorId).GreaterThan(0);
        RuleFor(x => x.BeneficiaryId).GreaterThan(0);
        RuleFor(x => x.SponsorshipType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MonthlyAmountPhp).GreaterThan(0);
        RuleFor(x => x.CaseWorker).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
