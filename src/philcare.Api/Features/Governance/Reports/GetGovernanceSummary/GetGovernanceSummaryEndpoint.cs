using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Reports.GetGovernanceSummary;

public sealed record GovernanceSummaryRow(
    int OrgBodyId, string OrgBodyName, int CurrentMemberCount, int MeetingsHeld,
    int MinutesPublished, int MinutesPending, int OpenDecisions);

public sealed class GetGovernanceSummaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/governance-summary", async (DateTime? from, DateTime? to, AppDbContext db, CancellationToken ct) =>
        {
            var bodies = await db.OrgBodies
                .Where(b => b.IsActive)
                .Select(b => new { b.Id, b.Name })
                .ToListAsync(ct);

            var assignmentQuery = db.Assignments.Where(a => a.Status == AssignmentStatus.Current);
            var currentMembers = await assignmentQuery
                .Select(a => new { a.OrgBodyId })
                .ToListAsync(ct);

            var meetingQuery = db.Meetings.Where(m => m.Status == MeetingStatus.Held);

            if (from is not null)
            {
                meetingQuery = meetingQuery.Where(m => m.MeetingDate >= from);
            }

            if (to is not null)
            {
                meetingQuery = meetingQuery.Where(m => m.MeetingDate <= to);
            }

            var heldMeetings = await meetingQuery
                .Select(m => new { m.Id, m.OrgBodyId })
                .ToListAsync(ct);

            // Materialize flat rows first, then group in-memory — EF Core cannot translate a
            // GroupBy with multiple aggregates in a single projection (see Sprint 2 lesson).
            var minutesRows = await db.MeetingMinutes
                .Select(mm => new { mm.Meeting.OrgBodyId, mm.PublicationStatus })
                .ToListAsync(ct);

            // decision_status lookup uses OPEN/IN_PROGRESS for unresolved decisions and
            // COMPLETED/CANCELLED for resolved ones — the first two count as "open" here.
            var decisionRows = await db.MeetingDecisions
                .Select(d => new { d.MeetingMinutes.Meeting.OrgBodyId, d.DecisionStatus })
                .ToListAsync(ct);

            var rows = bodies
                .Select(b => new GovernanceSummaryRow(
                    b.Id,
                    b.Name,
                    currentMembers.Count(a => a.OrgBodyId == b.Id),
                    heldMeetings.Count(m => m.OrgBodyId == b.Id),
                    minutesRows.Count(mm => mm.OrgBodyId == b.Id && mm.PublicationStatus == MinutesStatus.Published),
                    minutesRows.Count(mm => mm.OrgBodyId == b.Id && mm.PublicationStatus != MinutesStatus.Published),
                    decisionRows.Count(d => d.OrgBodyId == b.Id && (d.DecisionStatus == "OPEN" || d.DecisionStatus == "IN_PROGRESS"))))
                .OrderBy(r => r.OrgBodyName)
                .ToList();

            return Results.Ok(rows);
        })
        .WithName("GetGovernanceSummary")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
