using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Minutes.CreateMinutes;

public sealed class CreateMinutesHandler(AppDbContext db)
{
    public async Task<Result<CreateMinutesResponse>> HandleAsync(int meetingId, CreateMinutesRequest request, CancellationToken cancellationToken)
    {
        var meeting = await db.Meetings.FirstOrDefaultAsync(m => m.Id == meetingId, cancellationToken);

        if (meeting is null)
        {
            return Result.Failure<CreateMinutesResponse>(Error.NotFound("Governance.MeetingNotFound", "Meeting not found."));
        }

        if (meeting.Status != MeetingStatus.Held)
        {
            return Result.Failure<CreateMinutesResponse>(
                Error.Validation("Governance.MeetingNotHeld", "Minutes can only be recorded for a meeting whose status is Held."));
        }

        var minutesAlreadyExist = await db.MeetingMinutes.AnyAsync(mm => mm.MeetingId == meetingId, cancellationToken);

        if (minutesAlreadyExist)
        {
            return Result.Failure<CreateMinutesResponse>(Error.Conflict("Governance.MinutesAlreadyExist", "Minutes already exist for this meeting."));
        }

        if (request.PreparedByPersonId is not null)
        {
            var preparerExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.PreparedByPersonId, cancellationToken);

            if (!preparerExists)
            {
                return Result.Failure<CreateMinutesResponse>(Error.NotFound("Governance.PreparedByPersonNotFound", "Preparer not found."));
            }
        }

        if (request.ApprovedByPersonId is not null)
        {
            var approverExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.ApprovedByPersonId, cancellationToken);

            if (!approverExists)
            {
                return Result.Failure<CreateMinutesResponse>(Error.NotFound("Governance.ApprovedByPersonNotFound", "Approver not found."));
            }
        }

        var minutes = new MeetingMinutes
        {
            MeetingId = meetingId,
            PreparedByPersonId = request.PreparedByPersonId,
            ApprovedByPersonId = request.ApprovedByPersonId,
            Summary = request.Summary,
            NextMeetingDate = request.NextMeetingDate,
            DocumentLink = request.DocumentLink,
            PublicationStatus = MinutesStatus.Draft
        };

        db.MeetingMinutes.Add(minutes);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateMinutesResponse(minutes.Id, minutes.MeetingId, minutes.PublicationStatus.ToString()));
    }
}
