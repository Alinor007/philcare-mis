using FluentValidation;

namespace philcare.Api.Features.Programs.ActivityParticipants.AddActivityParticipant;

public sealed class AddActivityParticipantValidator : AbstractValidator<AddActivityParticipantRequest>
{
    public AddActivityParticipantValidator()
    {
        RuleFor(x => x.StaffMemberId).GreaterThan(0);
        RuleFor(x => x.RoleInActivity).MaximumLength(100);
        RuleFor(x => x.AttendanceStatus).MaximumLength(50);
        RuleFor(x => x.EvidenceLink).MaximumLength(500);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}
