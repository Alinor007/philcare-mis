using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Distributions.VoidDistribution;

public sealed class VoidDistributionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/distributions/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var distribution = await db.Distributions.FirstOrDefaultAsync(d => d.Id == id, ct);

            if (distribution is null)
            {
                return Results.Problem(title: "Distributions.NotFound", detail: "Distribution not found.", statusCode: StatusCodes.Status404NotFound);
            }

            if (distribution.IsVoided)
            {
                return Results.Problem(
                    title: "Distributions.AlreadyVoided", detail: "This distribution has already been voided.", statusCode: StatusCodes.Status409Conflict);
            }

            distribution.IsVoided = true;
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("VoidDistribution")
        .WithTags("Distributions")
        .RequireAuthorization("Admin");
    }
}
