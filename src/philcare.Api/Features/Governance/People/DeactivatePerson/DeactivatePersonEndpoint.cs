using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.People.DeactivatePerson;

public sealed class DeactivatePersonEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/governance/people/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var person = await db.GovernancePeople.FirstOrDefaultAsync(p => p.Id == id, ct);

            if (person is null)
            {
                return Results.Problem(title: "Governance.PersonNotFound", detail: "Person not found.", statusCode: StatusCodes.Status404NotFound);
            }

            person.IsActive = false;
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeactivatePerson")
        .WithTags("Governance")
        .RequireAuthorization("Admin");
    }
}
