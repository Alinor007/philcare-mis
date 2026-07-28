using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Reports.GetIncomeSummary;

public sealed record IncomeTypeSummaryRow(string IncomeType, int Count, decimal TotalPhp);

public sealed record IncomeSummaryResponse(List<IncomeTypeSummaryRow> ByType, decimal GrandTotalPhp);

public sealed class GetIncomeSummaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/income-summary", async (int? year, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.OtherIncomes.Where(i => !i.IsVoided);

            if (year is not null)
            {
                query = query.Where(i => i.ReportingYear == year);
            }

            // Materialize flat rows first, then group in-memory (EF GroupBy translation lesson from Sprint 2).
            var incomes = await query.Select(i => new { i.IncomeType, i.AmountPhp }).ToListAsync(ct);

            var byType = incomes
                .GroupBy(i => i.IncomeType)
                .Select(g => new IncomeTypeSummaryRow(g.Key, g.Count(), g.Sum(i => i.AmountPhp)))
                .OrderBy(r => r.IncomeType)
                .ToList();

            var grandTotal = incomes.Sum(i => i.AmountPhp);

            return Results.Ok(new IncomeSummaryResponse(byType, grandTotal));
        })
        .WithName("GetIncomeSummary")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
