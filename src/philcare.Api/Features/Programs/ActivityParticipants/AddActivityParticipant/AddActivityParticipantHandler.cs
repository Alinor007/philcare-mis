using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.ActivityParticipants.AddActivityParticipant;

public sealed class AddActivityParticipantHandler(AppDbContext db)
{
    public async Task<Result<AddActivityParticipantResponse>> HandleAsync(
        int activityId, AddActivityParticipantRequest request, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity is null)
        {
            return Result.Failure<AddActivityParticipantResponse>(Error.NotFound("ActivityParticipants.ActivityNotFound", "Activity not found."));
        }

        var participant = await db.Participants.FirstOrDefaultAsync(p => p.Id == request.ParticipantId, cancellationToken);

        if (participant is null)
        {
            return Result.Failure<AddActivityParticipantResponse>(Error.NotFound("ActivityParticipants.ParticipantNotFound", "Participant not found."));
        }

        var alreadyEnrolled = await db.ActivityParticipants
            .AnyAsync(ap => ap.ActivityId == activityId && ap.ParticipantId == request.ParticipantId, cancellationToken);

        if (alreadyEnrolled)
        {
            return Result.Failure<AddActivityParticipantResponse>(
                Error.Conflict("ActivityParticipants.AlreadyEnrolled", "This participant is already enrolled in this activity."));
        }

        var link = new ActivityParticipant
        {
            ActivityId = activityId,
            ParticipantId = request.ParticipantId,
            RoleInActivity = request.RoleInActivity,
            AttendanceStatus = request.AttendanceStatus,
            ConsentRequired = request.ConsentRequired,
            EvidenceLink = request.EvidenceLink,
            Remarks = request.Remarks
        };

        db.ActivityParticipants.Add(link);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new AddActivityParticipantResponse(
            link.Id, link.ActivityId, link.ParticipantId, participant.FullName, link.RoleInActivity, link.AttendanceStatus));
    }
}
