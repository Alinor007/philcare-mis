using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Reports.GetProgramSummary;

public sealed record ProgramSummaryRow(
    int ProgramId, string ProgramName, int ProjectCount, int ActivityCount,
    decimal TotalProjectBudget, decimal TotalActivityBudget, decimal TotalDistributedValuePhp);

public sealed class GetProgramSummaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/program-summary", async (AppDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Programs
                .OrderBy(p => p.Name)
                .Select(p => new ProgramSummaryRow(
                    p.Id,
                    p.Name,
                    p.Projects.Count,
                    p.Projects.SelectMany(pr => pr.Activities).Count(),
                    p.Projects.Sum(pr => pr.TotalBudget),
                    p.Projects.SelectMany(pr => pr.Activities).Sum(a => a.Budget),
                    p.Projects.SelectMany(pr => pr.Activities).SelectMany(a => a.Distributions).Where(d => !d.IsVoided).Sum(d => d.TotalValuePhp)))
                .ToListAsync(ct);

            return Results.Ok(rows);
        })
        .WithName("GetProgramSummary")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
