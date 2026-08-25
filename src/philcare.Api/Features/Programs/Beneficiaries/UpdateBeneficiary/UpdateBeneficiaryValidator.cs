using FluentValidation;

namespace philcare.Api.Features.Programs.Beneficiaries.UpdateBeneficiary;

public sealed class UpdateBeneficiaryValidator : AbstractValidator<UpdateBeneficiaryRequest>
{
    public UpdateBeneficiaryValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BeneficiaryType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Barangay).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Province).MaximumLength(100);
        RuleFor(x => x.Region).MaximumLength(50);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.VulnerabilityCategory).MaximumLength(50);
        RuleFor(x => x.SafeguardingCategory).MaximumLength(50);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Remarks).MaximumLength(1000);
        RuleFor(x => x.PhotoUrl).MaximumLength(500);
    }
}
