using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.MeetingParticipants.RemoveMeetingParticipant;

public sealed class RemoveMeetingParticipantEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/governance/meetings/{meetingId:int}/participants/{personId:int}", async (
            int meetingId, int personId, AppDbContext db, CancellationToken ct) =>
        {
            var participant = await db.MeetingParticipants
                .FirstOrDefaultAsync(mp => mp.MeetingId == meetingId && mp.PersonId == personId, ct);

            if (participant is null)
            {
                return Results.Problem(
                    title: "Governance.NotAParticipant", detail: "This person is not a participant in this meeting.", statusCode: StatusCodes.Status404NotFound);
            }

            db.MeetingParticipants.Remove(participant);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("RemoveMeetingParticipant")
        .WithTags("Governance")
        .RequireAuthorization("Admin");
    }
}
