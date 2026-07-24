using FluentValidation;

namespace philcare.Api.Features.Programs.Distributions.CreateDistribution;

public sealed class CreateDistributionValidator : AbstractValidator<CreateDistributionRequest>
{
    public CreateDistributionValidator()
    {
        RuleFor(x => x.DistributionType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ParticipantId).GreaterThan(0);
        RuleFor(x => x.FundingBucketCode).MaximumLength(20);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.TotalValuePhp).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.ProcessedBy).MaximumLength(200);
        RuleFor(x => x.ZakatAsnaf).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
