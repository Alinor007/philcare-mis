using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.Meetings.UpdateMeeting;

public sealed class UpdateMeetingHandler(AppDbContext db)
{
    public async Task<Result<UpdateMeetingResponse>> HandleAsync(int id, UpdateMeetingRequest request, CancellationToken cancellationToken)
    {
        var meeting = await db.Meetings.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (meeting is null)
        {
            return Result.Failure<UpdateMeetingResponse>(Error.NotFound("Governance.MeetingNotFound", "Meeting not found."));
        }

        if (request.ChairPersonId is not null)
        {
            var chairExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.ChairPersonId, cancellationToken);

            if (!chairExists)
            {
                return Result.Failure<UpdateMeetingResponse>(Error.NotFound("Governance.ChairPersonNotFound", "Chair person not found."));
            }
        }

        if (request.SecretaryPersonId is not null)
        {
            var secretaryExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.SecretaryPersonId, cancellationToken);

            if (!secretaryExists)
            {
                return Result.Failure<UpdateMeetingResponse>(Error.NotFound("Governance.SecretaryPersonNotFound", "Secretary person not found."));
            }
        }

        meeting.MeetingType = request.MeetingType;
        meeting.MeetingDate = request.MeetingDate;
        meeting.Mode = request.Mode;
        meeting.CalledBy = request.CalledBy;
        meeting.ChairPersonId = request.ChairPersonId;
        meeting.SecretaryPersonId = request.SecretaryPersonId;
        meeting.Status = request.Status;
        meeting.PublicationDeadline = request.PublicationDeadline;
        meeting.Notes = request.Notes;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateMeetingResponse(meeting.Id, meeting.OrgBodyId, meeting.MeetingType, meeting.MeetingDate, meeting.Status.ToString()));
    }
}
