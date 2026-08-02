using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.Meetings.GetMeetingQuorum;

// Reports counts alongside the meeting's snapshot policy rule text — it does NOT attempt to
// evaluate free-text rules like "50% + 1 ordinary; 2/3 strategic decisions". Interpreting that
// is a human judgment call the system surfaces the numbers for, not something it fakes.
public sealed record MeetingQuorumResponse(
    int MeetingId,
    int EligibleCount,
    int PresentCount,
    int CountsForQuorumPresentCount,
    double? PresentPercentage,
    string? QuorumRequired,
    string? DecisionThreshold);

public sealed class GetMeetingQuorumEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/meetings/{id:int}/quorum", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var meeting = await db.Meetings.FirstOrDefaultAsync(m => m.Id == id, ct);

            if (meeting is null)
            {
                return Results.Problem(title: "Governance.MeetingNotFound", detail: "Meeting not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var participants = await db.MeetingParticipants
                .Where(mp => mp.MeetingId == id)
                .Select(mp => new { mp.CountsForQuorum, mp.AttendanceStatus })
                .ToListAsync(ct);

            var eligibleCount = participants.Count(p => p.CountsForQuorum);
            var presentCount = participants.Count(p => p.AttendanceStatus == "PRESENT" || p.AttendanceStatus == "ONLINE_PRESENT");
            var countsForQuorumPresentCount = participants.Count(
                p => p.CountsForQuorum && (p.AttendanceStatus == "PRESENT" || p.AttendanceStatus == "ONLINE_PRESENT"));

            double? presentPercentage = eligibleCount > 0 ? Math.Round(countsForQuorumPresentCount * 100.0 / eligibleCount, 1) : null;

            return Results.Ok(new MeetingQuorumResponse(
                id, eligibleCount, presentCount, countsForQuorumPresentCount, presentPercentage, meeting.QuorumRequired, meeting.DecisionThreshold));
        })
        .WithName("GetMeetingQuorum")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
