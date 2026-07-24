using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.AidPrograms.DeactivateProgram;

public sealed class DeactivateProgramEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/programs/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var program = await db.Programs.FirstOrDefaultAsync(p => p.Id == id, ct);

            if (program is null)
            {
                return Results.Problem(title: "Programs.NotFound", detail: "Program not found.", statusCode: StatusCodes.Status404NotFound);
            }

            program.IsActive = false;
            program.Status = "Inactive";
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeactivateProgram")
        .WithTags("Programs")
        .RequireAuthorization("Admin");
    }
}
