using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Reports.GetZakatAmilReport;

public sealed record ZakatAmilReportResponse(
    decimal TotalZakatCollectedPhp, decimal MaxAmilSharePhp, decimal AmilAllocatedPhp, decimal AmilExpensedPhp, decimal AmilRemainingPhp);

public sealed class GetZakatAmilReportEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/zakat-amil", async (AppDbContext db, CancellationToken ct) =>
        {
            var totalZakatCollected = await db.Donations
                .Where(d => !d.IsVoided && d.FundCode == FinanceRules.ZakatFundCode)
                .SumAsync(d => d.AmountPhp, ct);

            var maxAmilShare = Math.Round(totalZakatCollected * FinanceRules.MaxAmilRate, 2);

            var amilBucket = await db.FundingBuckets.FirstOrDefaultAsync(b => b.Code == FinanceRules.ZakatAmilBucket, ct);

            var allocated = amilBucket?.AllocatedAmount ?? 0m;
            var expensed = amilBucket?.ExpensedAmount ?? 0m;

            return Results.Ok(new ZakatAmilReportResponse(totalZakatCollected, maxAmilShare, allocated, expensed, allocated - expensed));
        })
        .WithName("GetZakatAmilReport")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
