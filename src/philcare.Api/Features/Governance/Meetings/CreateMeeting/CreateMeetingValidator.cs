using FluentValidation;

namespace philcare.Api.Features.Governance.Meetings.CreateMeeting;

public sealed class CreateMeetingValidator : AbstractValidator<CreateMeetingRequest>
{
    public CreateMeetingValidator()
    {
        RuleFor(x => x.OrgBodyId).GreaterThan(0);
        RuleFor(x => x.MeetingType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Mode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CalledBy).MaximumLength(100);
        RuleFor(x => x.ChairPersonId).GreaterThan(0).When(x => x.ChairPersonId is not null);
        RuleFor(x => x.SecretaryPersonId).GreaterThan(0).When(x => x.SecretaryPersonId is not null);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
