using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.FundBuckets.GetFundBuckets;

public sealed record FundBucketResponse(
    int Id, string Name, string FundType, decimal TotalReceived, decimal AdminAllocated,
    decimal ProgramAllocated, decimal TotalExpensed, decimal Balance);

public sealed class GetFundBucketsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/fund-buckets", async (AppDbContext db, CancellationToken ct) =>
        {
            var buckets = await db.FundBuckets
                .OrderBy(b => b.FundType)
                .Select(b => new FundBucketResponse(
                    b.Id, b.Name, b.FundType, b.TotalReceived, b.AdminAllocated, b.ProgramAllocated, b.TotalExpensed,
                    b.ProgramAllocated - b.TotalExpensed))
                .ToListAsync(ct);

            return Results.Ok(buckets);
        })
        .WithName("GetFundBuckets")
        .WithTags("FundBuckets")
        .RequireAuthorization();
    }
}
