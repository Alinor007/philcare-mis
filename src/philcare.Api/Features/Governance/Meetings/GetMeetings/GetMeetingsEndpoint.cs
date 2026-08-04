using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Meetings.GetMeetings;

public sealed record MeetingListItemResponse(
    int Id, int OrgBodyId, string OrgBodyName, string MeetingType, DateTime MeetingDate, string Status, bool HasMinutes);

public sealed class GetMeetingsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/meetings", async (
            int? bodyId, string? meetingType, MeetingStatus? status, DateTime? from, DateTime? to, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Meetings.Include(m => m.OrgBody).Include(m => m.Minutes).AsQueryable();

            if (bodyId is not null)
            {
                query = query.Where(m => m.OrgBodyId == bodyId);
            }

            if (!string.IsNullOrWhiteSpace(meetingType))
            {
                query = query.Where(m => m.MeetingType == meetingType);
            }

            if (status is not null)
            {
                query = query.Where(m => m.Status == status);
            }

            if (from is not null)
            {
                query = query.Where(m => m.MeetingDate >= from);
            }

            if (to is not null)
            {
                query = query.Where(m => m.MeetingDate <= to);
            }

            var meetings = await query
                .OrderByDescending(m => m.MeetingDate)
                .Select(m => new MeetingListItemResponse(m.Id, m.OrgBodyId, m.OrgBody.Name, m.MeetingType, m.MeetingDate, m.Status.ToString(), m.Minutes != null))
                .ToListAsync(ct);

            return Results.Ok(meetings);
        })
        .WithName("GetMeetings")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
