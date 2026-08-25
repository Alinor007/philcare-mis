using FluentValidation;

namespace philcare.Api.Features.Programs.DistributionBeneficiaries.AddDistributionBeneficiary;

public sealed class AddDistributionBeneficiaryValidator : AbstractValidator<AddDistributionBeneficiaryRequest>
{
    public AddDistributionBeneficiaryValidator()
    {
        RuleFor(x => x.BeneficiaryId).GreaterThan(0);
        RuleFor(x => x.EvidenceLink).MaximumLength(500);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}
