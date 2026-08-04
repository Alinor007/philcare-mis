using FluentValidation;

namespace philcare.Api.Features.Finance.DonorEngagements.CreateDonorEngagement;

public sealed class CreateDonorEngagementValidator : AbstractValidator<CreateDonorEngagementRequest>
{
    public CreateDonorEngagementValidator()
    {
        RuleFor(x => x.DonorId).GreaterThan(0);
        RuleFor(x => x.EngagementType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.FollowUpDate)
            .NotNull()
            .When(x => x.FollowUpRequired)
            .WithMessage("Follow-up date is required when follow-up is flagged.");
    }
}
