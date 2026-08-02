using FluentValidation;

namespace philcare.Api.Features.Finance.DonorEngagements.UpdateDonorEngagement;

public sealed class UpdateDonorEngagementValidator : AbstractValidator<UpdateDonorEngagementRequest>
{
    public UpdateDonorEngagementValidator()
    {
        RuleFor(x => x.EngagementType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.FollowUpDate)
            .NotNull()
            .When(x => x.FollowUpRequired)
            .WithMessage("Follow-up date is required when follow-up is flagged.");
    }
}
