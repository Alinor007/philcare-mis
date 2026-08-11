using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.ActivityParticipants.GetActivityParticipants;

/// <summary>A staffing-roster row: which staff member, in what job, in what activity role.</summary>
public sealed record ActivityRosterRow(
    int StaffMemberId, string StaffMemberName, string Position, string? RoleInActivity, string? AttendanceStatus, bool IsActive);

public sealed class GetActivityParticipantsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/activities/{activityId:int}/participants", async (
            int activityId, bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var activityExists = await db.Activities.AnyAsync(a => a.Id == activityId, ct);

            if (!activityExists)
            {
                return Results.Problem(title: "Activities.NotFound", detail: "Activity not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var query = db.ActivityParticipants.Where(ap => ap.ActivityId == activityId);

            if (includeInactive != true)
            {
                query = query.Where(ap => ap.IsActive);
            }

            var roster = await query
                .Include(ap => ap.StaffMember)
                .OrderBy(ap => ap.StaffMember.FullName)
                .Select(ap => new ActivityRosterRow(
                    ap.StaffMemberId, ap.StaffMember.FullName, ap.StaffMember.Position, ap.RoleInActivity, ap.AttendanceStatus, ap.IsActive))
                .ToListAsync(ct);

            return Results.Ok(roster);
        })
        .WithName("GetActivityParticipants")
        .WithTags("ActivityParticipants")
        .RequireAuthorization();
    }
}
