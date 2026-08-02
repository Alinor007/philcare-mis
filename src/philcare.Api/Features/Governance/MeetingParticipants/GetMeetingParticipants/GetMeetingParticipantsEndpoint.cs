using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.MeetingParticipants.GetMeetingParticipants;

public sealed record MeetingParticipantRosterRow(
    int PersonId, string PersonFullName, string? RoleInMeeting, string AttendanceStatus, bool VotingRight, bool CountsForQuorum);

public sealed class GetMeetingParticipantsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/meetings/{meetingId:int}/participants", async (int meetingId, AppDbContext db, CancellationToken ct) =>
        {
            var meetingExists = await db.Meetings.AnyAsync(m => m.Id == meetingId, ct);

            if (!meetingExists)
            {
                return Results.Problem(title: "Governance.MeetingNotFound", detail: "Meeting not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var roster = await db.MeetingParticipants
                .Where(mp => mp.MeetingId == meetingId)
                .Include(mp => mp.Person)
                .OrderBy(mp => mp.Person.FullName)
                .Select(mp => new MeetingParticipantRosterRow(
                    mp.PersonId, mp.Person.FullName, mp.RoleInMeeting, mp.AttendanceStatus, mp.VotingRight, mp.CountsForQuorum))
                .ToListAsync(ct);

            return Results.Ok(roster);
        })
        .WithName("GetMeetingParticipants")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
