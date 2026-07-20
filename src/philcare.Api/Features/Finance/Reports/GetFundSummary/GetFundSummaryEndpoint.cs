using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Reports.GetFundSummary;

public sealed record FundSummaryBucketResponse(
    string FundType, decimal TotalReceived, decimal AdminAllocated, decimal ProgramAllocated,
    decimal TotalExpensed, decimal Balance);

public sealed record FundSummaryResponse(
    List<FundSummaryBucketResponse> Buckets,
    decimal GrandTotalReceived,
    decimal GrandTotalAdminAllocated,
    decimal GrandTotalProgramAllocated,
    decimal GrandTotalExpensed,
    decimal GrandTotalBalance,
    decimal OverallAdminRatio);

public sealed class GetFundSummaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/fund-summary", async (AppDbContext db, CancellationToken ct) =>
        {
            var buckets = await db.FundBuckets
                .OrderBy(b => b.FundType)
                .Select(b => new FundSummaryBucketResponse(
                    b.FundType, b.TotalReceived, b.AdminAllocated, b.ProgramAllocated, b.TotalExpensed,
                    b.ProgramAllocated - b.TotalExpensed))
                .ToListAsync(ct);

            var totalReceived = buckets.Sum(b => b.TotalReceived);
            var totalAdmin = buckets.Sum(b => b.AdminAllocated);
            var totalProgram = buckets.Sum(b => b.ProgramAllocated);
            var totalExpensed = buckets.Sum(b => b.TotalExpensed);
            var totalBalance = buckets.Sum(b => b.Balance);
            var adminRatio = totalReceived == 0 ? 0 : Math.Round(totalAdmin / totalReceived, 4);

            return Results.Ok(new FundSummaryResponse(
                buckets, totalReceived, totalAdmin, totalProgram, totalExpensed, totalBalance, adminRatio));
        })
        .WithName("GetFundSummary")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
