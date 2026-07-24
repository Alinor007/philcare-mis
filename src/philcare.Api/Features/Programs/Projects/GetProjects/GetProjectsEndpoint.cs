using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Projects.GetProjects;

public sealed record ProjectListItemResponse(
    int Id, int ProgramId, string ProgramName, string Name, decimal TotalBudget, string ImplementationStatus, bool IsActive);

public sealed class GetProjectsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects", async (
            int? programId,
            string? implementationStatus,
            bool? includeInactive,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var query = db.Projects.Include(p => p.Program).AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(p => p.IsActive);
            }

            if (programId is not null)
            {
                query = query.Where(p => p.ProgramId == programId);
            }

            if (!string.IsNullOrWhiteSpace(implementationStatus))
            {
                query = query.Where(p => p.ImplementationStatus == implementationStatus);
            }

            var projects = await query
                .OrderBy(p => p.Name)
                .Select(p => new ProjectListItemResponse(p.Id, p.ProgramId, p.Program.Name, p.Name, p.TotalBudget, p.ImplementationStatus, p.IsActive))
                .ToListAsync(ct);

            return Results.Ok(projects);
        })
        .WithName("GetProjects")
        .WithTags("Projects")
        .RequireAuthorization();
    }
}
