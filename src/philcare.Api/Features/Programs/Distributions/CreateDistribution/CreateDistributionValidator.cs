using FluentValidation;

namespace philcare.Api.Features.Programs.Distributions.CreateDistribution;

public sealed class CreateDistributionValidator : AbstractValidator<CreateDistributionRequest>
{
    public CreateDistributionValidator()
    {
        RuleFor(x => x.DistributionType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ActivityId).GreaterThan(0);
        RuleFor(x => x.FundingBucketCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Quantity).GreaterThan(0);
        // Zero-value in-kind handouts are allowed — CreateDistributionHandler skips the expense
        // post entirely when Quantity * UnitValuePhp rounds to 0, rather than rejecting it (unlike
        // CreateExpenseValidator's AmountOriginal, which must be > 0).
        RuleFor(x => x.UnitValuePhp).GreaterThanOrEqualTo(0);
        // No BeneficiaryCount rule — reach is derived from the roster, not sent by the client.
        // The "zakat asnaf is required" rule is conditional on the funding bucket's fund and type,
        // which needs a DB read, so it lives in the handler rather than here.
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.ProcessedBy).MaximumLength(200);
        RuleFor(x => x.PaymentMethod).MaximumLength(50);
        RuleFor(x => x.ZakatAsnaf).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
