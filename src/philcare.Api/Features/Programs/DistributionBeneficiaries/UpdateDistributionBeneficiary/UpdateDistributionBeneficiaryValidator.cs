using FluentValidation;

namespace philcare.Api.Features.Programs.DistributionBeneficiaries.UpdateDistributionBeneficiary;

public sealed class UpdateDistributionBeneficiaryValidator : AbstractValidator<UpdateDistributionBeneficiaryRequest>
{
    public UpdateDistributionBeneficiaryValidator()
    {
        RuleFor(x => x.EvidenceLink).MaximumLength(500);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}
