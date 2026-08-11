using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.HumanResources.Volunteers.ActivityVolunteers.RemoveActivityVolunteer;

public sealed class RemoveActivityVolunteerEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/activities/{activityId:int}/volunteers/{volunteerId:int}", async (
            int activityId, int volunteerId, AppDbContext db, CancellationToken ct) =>
        {
            var link = await db.ActivityVolunteers
                .FirstOrDefaultAsync(av => av.ActivityId == activityId && av.VolunteerId == volunteerId, ct);

            if (link is null)
            {
                return Results.Problem(
                    title: "ActivityVolunteers.NotFound", detail: "This volunteer is not enrolled in this activity.", statusCode: StatusCodes.Status404NotFound);
            }

            db.ActivityVolunteers.Remove(link);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("RemoveActivityVolunteer")
        .WithTags("ActivityVolunteers")
        .RequireAuthorization("Program");
    }
}
