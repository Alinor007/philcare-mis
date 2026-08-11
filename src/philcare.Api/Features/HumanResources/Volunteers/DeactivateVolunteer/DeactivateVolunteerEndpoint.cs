using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.HumanResources.Volunteers.DeactivateVolunteer;

public sealed class DeactivateVolunteerEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/volunteers/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var volunteer = await db.Volunteers.FirstOrDefaultAsync(v => v.Id == id, ct);

            if (volunteer is null)
            {
                return Results.Problem(title: "Volunteers.NotFound", detail: "Volunteer not found.", statusCode: StatusCodes.Status404NotFound);
            }

            volunteer.IsActive = false;
            volunteer.Status = "INACTIVE";
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeactivateVolunteer")
        .WithTags("Volunteers")
        .RequireAuthorization("Admin");
    }
}
