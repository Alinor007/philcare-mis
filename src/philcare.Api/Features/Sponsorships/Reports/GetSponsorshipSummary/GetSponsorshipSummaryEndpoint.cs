using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Sponsorships.Reports.GetSponsorshipSummary;

public sealed record SponsorshipSummaryRow(string SponsorshipType, string Status, int Count, decimal TotalMonthlyCommitmentPhp);

public sealed class GetSponsorshipSummaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/sponsorship-summary", async (AppDbContext db, CancellationToken ct) =>
        {
            // Materialize flat rows first, then group in-memory (EF GroupBy translation lesson from Sprint 2).
            var sponsorships = await db.Sponsorships
                .Select(s => new { s.SponsorshipType, Status = s.Status.ToString(), s.MonthlyAmountPhp })
                .ToListAsync(ct);

            var rows = sponsorships
                .GroupBy(s => new { s.SponsorshipType, s.Status })
                .Select(g => new SponsorshipSummaryRow(g.Key.SponsorshipType, g.Key.Status, g.Count(), g.Sum(s => s.MonthlyAmountPhp)))
                .OrderBy(r => r.SponsorshipType).ThenBy(r => r.Status)
                .ToList();

            return Results.Ok(rows);
        })
        .WithName("GetSponsorshipSummary")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
