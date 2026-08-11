using FluentValidation;

namespace philcare.Api.Features.Governance.MeetingParticipants.AddMeetingParticipant;

public sealed class AddMeetingParticipantValidator : AbstractValidator<AddMeetingParticipantRequest>
{
    public AddMeetingParticipantValidator()
    {
        RuleFor(x => x.PersonId).GreaterThan(0);
        RuleFor(x => x.AssignmentId).GreaterThan(0).When(x => x.AssignmentId is not null);
        RuleFor(x => x.RoleInMeeting).MaximumLength(100);
        RuleFor(x => x.AttendanceStatus).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ParticipationMode).MaximumLength(50);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}
