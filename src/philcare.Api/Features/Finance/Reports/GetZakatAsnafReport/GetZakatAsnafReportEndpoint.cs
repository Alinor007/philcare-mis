using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Reports.GetZakatAsnafReport;

public sealed record ZakatAsnafReportRow(string ZakatAsnaf, decimal TotalAmount, int TotalBeneficiaries, int ExpenseCount);

public sealed class GetZakatAsnafReportEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/zakat-asnaf", async (AppDbContext db, CancellationToken ct) =>
        {
            var expenses = await db.Expenses
                .Where(e => !e.IsVoided && e.FundBucket.FundType == FinanceRules.ZakatFundType && e.ZakatAsnaf != null)
                .Select(e => new { e.ZakatAsnaf, e.Amount, e.BeneficiaryCount })
                .ToListAsync(ct);

            var rows = expenses
                .GroupBy(e => e.ZakatAsnaf!)
                .Select(g => new ZakatAsnafReportRow(g.Key, g.Sum(e => e.Amount), g.Sum(e => e.BeneficiaryCount ?? 0), g.Count()))
                .OrderBy(r => r.ZakatAsnaf)
                .ToList();

            return Results.Ok(rows);
        })
        .WithName("GetZakatAsnafReport")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
