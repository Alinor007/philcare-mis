using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.FundingBuckets.GetFundingBuckets;

public sealed record FundingBucketResponse(
    int Id, string Code, string Name, string FundCode, BucketType BucketType, decimal MaxAdminRate,
    decimal AllocatedAmount, decimal ExpensedAmount, decimal Remaining);

public sealed class GetFundingBucketsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/funding-buckets", async (string? fundCode, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.FundingBuckets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(fundCode))
            {
                query = query.Where(b => b.FundCode == fundCode);
            }

            var buckets = await query
                .OrderBy(b => b.FundCode).ThenBy(b => b.Code)
                .Select(b => new FundingBucketResponse(
                    b.Id, b.Code, b.Name, b.FundCode, b.BucketType, b.MaxAdminRate,
                    b.AllocatedAmount, b.ExpensedAmount, b.AllocatedAmount - b.ExpensedAmount))
                .ToListAsync(ct);

            return Results.Ok(buckets);
        })
        .WithName("GetFundingBuckets")
        .WithTags("FundingBuckets")
        .RequireAuthorization();
    }
}
