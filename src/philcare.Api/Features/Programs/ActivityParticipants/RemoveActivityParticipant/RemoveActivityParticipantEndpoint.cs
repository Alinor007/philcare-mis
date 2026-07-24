using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.ActivityParticipants.RemoveActivityParticipant;

public sealed class RemoveActivityParticipantEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/activities/{activityId:int}/participants/{participantId:int}", async (
            int activityId, int participantId, AppDbContext db, CancellationToken ct) =>
        {
            var link = await db.ActivityParticipants
                .FirstOrDefaultAsync(ap => ap.ActivityId == activityId && ap.ParticipantId == participantId, ct);

            if (link is null)
            {
                return Results.Problem(
                    title: "ActivityParticipants.NotFound", detail: "This participant is not enrolled in this activity.", statusCode: StatusCodes.Status404NotFound);
            }

            db.ActivityParticipants.Remove(link);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("RemoveActivityParticipant")
        .WithTags("ActivityParticipants")
        .RequireAuthorization("Program");
    }
}
