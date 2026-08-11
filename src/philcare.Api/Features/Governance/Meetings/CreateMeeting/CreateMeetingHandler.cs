using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Meetings.CreateMeeting;

public sealed class CreateMeetingHandler(AppDbContext db)
{
    public async Task<Result<CreateMeetingResponse>> HandleAsync(CreateMeetingRequest request, CancellationToken cancellationToken)
    {
        var body = await db.OrgBodies.FirstOrDefaultAsync(b => b.Id == request.OrgBodyId, cancellationToken);

        if (body is null)
        {
            return Result.Failure<CreateMeetingResponse>(Error.NotFound("Governance.BodyNotFound", "Governance body not found."));
        }

        if (!body.IsActive)
        {
            return Result.Failure<CreateMeetingResponse>(Error.Validation("Governance.BodyInactive", "Cannot schedule a meeting for an inactive governance body."));
        }

        if (request.ChairPersonId is not null)
        {
            var chairExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.ChairPersonId, cancellationToken);

            if (!chairExists)
            {
                return Result.Failure<CreateMeetingResponse>(Error.NotFound("Governance.ChairPersonNotFound", "Chair person not found."));
            }
        }

        if (request.SecretaryPersonId is not null)
        {
            var secretaryExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.SecretaryPersonId, cancellationToken);

            if (!secretaryExists)
            {
                return Result.Failure<CreateMeetingResponse>(Error.NotFound("Governance.SecretaryPersonNotFound", "Secretary person not found."));
            }
        }

        var meeting = new Meeting
        {
            OrgBodyId = request.OrgBodyId,
            MeetingType = request.MeetingType,
            MeetingDate = request.MeetingDate,
            Mode = request.Mode,
            CalledBy = request.CalledBy,
            ChairPersonId = request.ChairPersonId,
            SecretaryPersonId = request.SecretaryPersonId,
            // Snapshot the body's current policy so a later change to it doesn't rewrite history.
            QuorumRequired = body.QuorumRule,
            DecisionThreshold = body.DecisionThreshold,
            Status = MeetingStatus.Scheduled,
            PublicationDeadline = request.PublicationDeadline ?? request.MeetingDate.AddDays(10),
            Notes = request.Notes
        };

        db.Meetings.Add(meeting);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateMeetingResponse(
            meeting.Id, meeting.OrgBodyId, meeting.MeetingType, meeting.MeetingDate, meeting.Status.ToString(),
            meeting.QuorumRequired, meeting.DecisionThreshold));
    }
}
