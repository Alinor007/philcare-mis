using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Volunteers.ActivityVolunteers.GetActivityVolunteers;

public sealed record ActivityVolunteerRosterRow(
    int VolunteerId, string VolunteerName, string? RoleInActivity, string? AttendanceStatus, decimal? HoursServed);

public sealed class GetActivityVolunteersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/activities/{activityId:int}/volunteers", async (int activityId, AppDbContext db, CancellationToken ct) =>
        {
            var activityExists = await db.Activities.AnyAsync(a => a.Id == activityId, ct);

            if (!activityExists)
            {
                return Results.Problem(title: "Activities.NotFound", detail: "Activity not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var roster = await db.ActivityVolunteers
                .Where(av => av.ActivityId == activityId)
                .Include(av => av.Volunteer)
                .OrderBy(av => av.Volunteer.FullName)
                .Select(av => new ActivityVolunteerRosterRow(
                    av.VolunteerId, av.Volunteer.FullName, av.RoleInActivity, av.AttendanceStatus, av.HoursServed))
                .ToListAsync(ct);

            return Results.Ok(roster);
        })
        .WithName("GetActivityVolunteers")
        .WithTags("ActivityVolunteers")
        .RequireAuthorization();
    }
}
