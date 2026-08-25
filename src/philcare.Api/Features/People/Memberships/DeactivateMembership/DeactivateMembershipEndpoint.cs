using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.People.Memberships.DeactivateMembership;

public sealed class DeactivateMembershipEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        // DELETE verb, soft-close semantics — the membership roll is history and is never
        // hard-deleted. Mirrors DeactivateVolunteer in also moving Status, so a closed row does
        // not keep reporting itself as ACTIVE, and stamps ExitDate if the caller never set one.
        app.MapDelete("/api/memberships/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == id, ct);

            if (membership is null)
            {
                return Results.Problem(title: "Memberships.NotFound", detail: "Membership not found.", statusCode: StatusCodes.Status404NotFound);
            }

            membership.IsActive = false;
            membership.Status = "RESIGNED";
            membership.ExitDate ??= DateTime.UtcNow.Date;

            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeactivateMembership")
        .WithTags("Memberships")
        .RequireAuthorization("Admin");
    }
}
