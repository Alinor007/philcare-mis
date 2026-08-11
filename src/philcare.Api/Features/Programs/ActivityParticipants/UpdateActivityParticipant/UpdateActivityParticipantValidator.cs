using FluentValidation;

namespace philcare.Api.Features.Programs.ActivityParticipants.UpdateActivityParticipant;

public sealed class UpdateActivityParticipantValidator : AbstractValidator<UpdateActivityParticipantRequest>
{
    public UpdateActivityParticipantValidator()
    {
        RuleFor(x => x.RoleInActivity).MaximumLength(100);
        RuleFor(x => x.AttendanceStatus).MaximumLength(50);
        RuleFor(x => x.EvidenceLink).MaximumLength(500);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}
