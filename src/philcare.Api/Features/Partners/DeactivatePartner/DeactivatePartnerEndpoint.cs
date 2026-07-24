using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Partners.DeactivatePartner;

public sealed class DeactivatePartnerEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/partners/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var partner = await db.Partners.FirstOrDefaultAsync(p => p.Id == id, ct);

            if (partner is null)
            {
                return Results.Problem(title: "Partners.NotFound", detail: "Partner not found.", statusCode: StatusCodes.Status404NotFound);
            }

            partner.IsActive = false;
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeactivatePartner")
        .WithTags("Partners")
        .RequireAuthorization("Admin");
    }
}
