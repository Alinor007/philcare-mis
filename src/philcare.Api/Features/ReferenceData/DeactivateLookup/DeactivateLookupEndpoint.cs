using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.ReferenceData.DeactivateLookup;

public sealed class DeactivateLookupEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/lookups/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var item = await db.LookupItems.FirstOrDefaultAsync(l => l.Id == id, ct);

            if (item is null)
            {
                return Results.Problem(title: "Lookup.NotFound", detail: "Lookup item not found.", statusCode: StatusCodes.Status404NotFound);
            }

            item.IsActive = false;
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeactivateLookup")
        .WithTags("ReferenceData")
        .RequireAuthorization("Admin");
    }
}
