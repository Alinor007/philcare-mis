using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.ActivityParticipants.GetActivityParticipants;

public sealed record ActivityRosterRow(
    int ParticipantId, string ParticipantName, string ParticipantType, string? RoleInActivity, string? AttendanceStatus);

public sealed class GetActivityParticipantsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/activities/{activityId:int}/participants", async (int activityId, AppDbContext db, CancellationToken ct) =>
        {
            var activityExists = await db.Activities.AnyAsync(a => a.Id == activityId, ct);

            if (!activityExists)
            {
                return Results.Problem(title: "Activities.NotFound", detail: "Activity not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var roster = await db.ActivityParticipants
                .Where(ap => ap.ActivityId == activityId)
                .Include(ap => ap.Participant)
                .OrderBy(ap => ap.Participant.FullName)
                .Select(ap => new ActivityRosterRow(
                    ap.ParticipantId, ap.Participant.FullName, ap.Participant.ParticipantType, ap.RoleInActivity, ap.AttendanceStatus))
                .ToListAsync(ct);

            return Results.Ok(roster);
        })
        .WithName("GetActivityParticipants")
        .WithTags("ActivityParticipants")
        .RequireAuthorization();
    }
}
