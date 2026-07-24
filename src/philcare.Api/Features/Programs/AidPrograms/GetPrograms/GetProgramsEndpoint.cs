using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.AidPrograms.GetPrograms;

public sealed record ProgramListItemResponse(int Id, string Name, string Category, string Status, bool IsActive);

public sealed class GetProgramsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/programs", async (bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Programs.AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(p => p.IsActive);
            }

            var programs = await query
                .OrderBy(p => p.Name)
                .Select(p => new ProgramListItemResponse(p.Id, p.Name, p.Category, p.Status, p.IsActive))
                .ToListAsync(ct);

            return Results.Ok(programs);
        })
        .WithName("GetPrograms")
        .WithTags("Programs")
        .RequireAuthorization();
    }
}
