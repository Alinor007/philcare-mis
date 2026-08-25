using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.MeetingParticipants.AddMeetingParticipant;

public sealed class AddMeetingParticipantHandler(AppDbContext db)
{
    public async Task<Result<AddMeetingParticipantResponse>> HandleAsync(
        int meetingId, AddMeetingParticipantRequest request, CancellationToken cancellationToken)
    {
        var meetingExists = await db.Meetings.AnyAsync(m => m.Id == meetingId, cancellationToken);

        if (!meetingExists)
        {
            return Result.Failure<AddMeetingParticipantResponse>(Error.NotFound("Governance.MeetingNotFound", "Meeting not found."));
        }

        var person = await db.GovernancePeople.FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);

        if (person is null)
        {
            return Result.Failure<AddMeetingParticipantResponse>(Error.NotFound("Governance.PersonNotFound", "Person not found."));
        }

        var alreadyABeneficiary = await db.MeetingParticipants
            .AnyAsync(mp => mp.MeetingId == meetingId && mp.PersonId == request.PersonId, cancellationToken);

        if (alreadyABeneficiary)
        {
            return Result.Failure<AddMeetingParticipantResponse>(
                Error.Conflict("Governance.AlreadyABeneficiary", "This person is already a beneficiary in this meeting."));
        }

        if (request.AssignmentId is not null)
        {
            var assignmentBelongsToPerson = await db.Assignments
                .AnyAsync(a => a.Id == request.AssignmentId && a.PersonId == request.PersonId, cancellationToken);

            if (!assignmentBelongsToPerson)
            {
                return Result.Failure<AddMeetingParticipantResponse>(
                    Error.Validation("Governance.AssignmentPersonMismatch", "The provided assignment does not belong to this person."));
            }
        }

        var beneficiary = new MeetingParticipant
        {
            MeetingId = meetingId,
            PersonId = request.PersonId,
            AssignmentId = request.AssignmentId,
            RoleInMeeting = request.RoleInMeeting,
            AttendanceStatus = request.AttendanceStatus,
            VotingRight = request.VotingRight,
            CountsForQuorum = request.CountsForQuorum,
            ParticipationMode = request.ParticipationMode,
            Remarks = request.Remarks
        };

        db.MeetingParticipants.Add(beneficiary);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new AddMeetingParticipantResponse(beneficiary.Id, beneficiary.MeetingId, beneficiary.PersonId, person.FullName, beneficiary.AttendanceStatus));
    }
}
