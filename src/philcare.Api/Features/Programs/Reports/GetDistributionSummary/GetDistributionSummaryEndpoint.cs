using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Reports.GetDistributionSummary;

public sealed record DistributionSummaryRow(
    string DistributionType, int DistributionCount, int DistinctBeneficiaries, int TotalQuantity, decimal TotalValuePhp);

public sealed class GetDistributionSummaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/distribution-summary", async (AppDbContext db, CancellationToken ct) =>
        {
            // Materialize flat rows first, then group in-memory — EF Core cannot translate a
            // GroupBy with multiple aggregates in a single projection (see Sprint 2 lesson).
            var distributions = await db.Distributions
                .Where(d => !d.IsVoided)
                .Select(d => new { d.Id, d.DistributionType, d.Quantity, d.TotalValuePhp })
                .ToListAsync(ct);

            // Distinct reach comes from the roster, not Distribution.BeneficiaryId — that FK names
            // only the primary recipient, so counting through it undercounts every event that
            // reached more than one person.
            var reach = await db.DistributionBeneficiaries
                .Where(r => r.IsActive && !r.Distribution.IsVoided)
                .Select(r => new { r.DistributionId, r.BeneficiaryId })
                .ToListAsync(ct);

            var recipientsByDistribution = reach
                .GroupBy(r => r.DistributionId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.BeneficiaryId).ToList());

            var rows = distributions
                .GroupBy(d => d.DistributionType)
                .Select(g => new DistributionSummaryRow(
                    g.Key,
                    g.Count(),
                    g.SelectMany(d => recipientsByDistribution.GetValueOrDefault(d.Id, [])).Distinct().Count(),
                    g.Sum(d => d.Quantity),
                    g.Sum(d => d.TotalValuePhp)))
                .OrderBy(r => r.DistributionType)
                .ToList();

            return Results.Ok(rows);
        })
        .WithName("GetDistributionSummary")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
