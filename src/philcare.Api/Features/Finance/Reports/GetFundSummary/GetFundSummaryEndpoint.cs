using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Reports.GetFundSummary;

public sealed record FundSummaryBucketRow(
    string FundCode, string BucketCode, string BucketName, decimal AllocatedAmount, decimal ExpensedAmount, decimal Remaining);

public sealed record FundSummaryResponse(
    List<FundSummaryBucketRow> Buckets,
    decimal GrandTotalAllocated,
    decimal GrandTotalExpensed,
    decimal GrandTotalRemaining,
    decimal OverallAdminRatio);

public sealed class GetFundSummaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/fund-summary", async (AppDbContext db, CancellationToken ct) =>
        {
            var buckets = await db.FundingBuckets
                .OrderBy(b => b.FundCode).ThenBy(b => b.Code)
                .Select(b => new FundSummaryBucketRow(
                    b.FundCode, b.Code, b.Name, b.AllocatedAmount, b.ExpensedAmount, b.AllocatedAmount - b.ExpensedAmount))
                .ToListAsync(ct);

            var totalAllocated = buckets.Sum(b => b.AllocatedAmount);
            var totalExpensed = buckets.Sum(b => b.ExpensedAmount);
            var totalRemaining = buckets.Sum(b => b.Remaining);

            var adminAllocated = await db.FundingBuckets
                .Where(b => b.BucketType == Domain.BucketType.Admin)
                .SumAsync(b => b.AllocatedAmount, ct);

            var adminRatio = totalAllocated == 0 ? 0 : Math.Round(adminAllocated / totalAllocated, 4);

            return Results.Ok(new FundSummaryResponse(buckets, totalAllocated, totalExpensed, totalRemaining, adminRatio));
        })
        .WithName("GetFundSummary")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
