using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.Minutes.GetMinutesByMeeting;

public sealed record MinutesDetailResponse(
    int Id,
    int MeetingId,
    int? PreparedByPersonId,
    string? PreparedByPersonName,
    int? ApprovedByPersonId,
    string? ApprovedByPersonName,
    string? Summary,
    DateTime? NextMeetingDate,
    string? DocumentLink,
    string PublicationStatus,
    int DecisionCount);

public sealed class GetMinutesByMeetingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/meetings/{meetingId:int}/minutes", async (int meetingId, AppDbContext db, CancellationToken ct) =>
        {
            var minutes = await db.MeetingMinutes
                .Where(mm => mm.MeetingId == meetingId)
                .Select(mm => new MinutesDetailResponse(
                    mm.Id, mm.MeetingId, mm.PreparedByPersonId, mm.PreparedByPerson == null ? null : mm.PreparedByPerson.FullName,
                    mm.ApprovedByPersonId, mm.ApprovedByPerson == null ? null : mm.ApprovedByPerson.FullName,
                    mm.Summary, mm.NextMeetingDate, mm.DocumentLink, mm.PublicationStatus.ToString(), mm.Decisions.Count))
                .FirstOrDefaultAsync(ct);

            if (minutes is null)
            {
                return Results.Problem(title: "Governance.MinutesNotFound", detail: "Minutes not found for this meeting.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(minutes);
        })
        .WithName("GetMinutesByMeeting")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
