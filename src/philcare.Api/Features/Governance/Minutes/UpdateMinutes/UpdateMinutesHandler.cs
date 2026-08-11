using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Minutes.UpdateMinutes;

public sealed class UpdateMinutesHandler(AppDbContext db)
{
    public async Task<Result<UpdateMinutesResponse>> HandleAsync(int meetingId, UpdateMinutesRequest request, CancellationToken cancellationToken)
    {
        var minutes = await db.MeetingMinutes.FirstOrDefaultAsync(mm => mm.MeetingId == meetingId, cancellationToken);

        if (minutes is null)
        {
            return Result.Failure<UpdateMinutesResponse>(Error.NotFound("Governance.MinutesNotFound", "Minutes not found for this meeting."));
        }

        if (minutes.PublicationStatus == MinutesStatus.Published)
        {
            return Result.Failure<UpdateMinutesResponse>(Error.Conflict("Governance.MinutesPublished", "Published minutes can no longer be edited."));
        }

        if (request.PreparedByPersonId is not null)
        {
            var preparerExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.PreparedByPersonId, cancellationToken);

            if (!preparerExists)
            {
                return Result.Failure<UpdateMinutesResponse>(Error.NotFound("Governance.PreparedByPersonNotFound", "Preparer not found."));
            }
        }

        if (request.ApprovedByPersonId is not null)
        {
            var approverExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.ApprovedByPersonId, cancellationToken);

            if (!approverExists)
            {
                return Result.Failure<UpdateMinutesResponse>(Error.NotFound("Governance.ApprovedByPersonNotFound", "Approver not found."));
            }
        }

        minutes.PreparedByPersonId = request.PreparedByPersonId;
        minutes.ApprovedByPersonId = request.ApprovedByPersonId;
        minutes.Summary = request.Summary;
        minutes.NextMeetingDate = request.NextMeetingDate;
        minutes.DocumentLink = request.DocumentLink;
        minutes.PublicationStatus = request.PublicationStatus;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateMinutesResponse(minutes.Id, minutes.MeetingId, minutes.PublicationStatus.ToString()));
    }
}
