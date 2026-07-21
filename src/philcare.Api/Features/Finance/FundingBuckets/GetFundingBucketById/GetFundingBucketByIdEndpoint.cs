using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.FundingBuckets.GetFundingBucketById;

public sealed record BucketAllocationResponse(
    int? DonationId, AllocationType AllocationType, decimal AllocatedAmountPhp, DateTime AllocationDate);

public sealed record BucketExpenseResponse(int Id, decimal AmountPhp, string ExpenseCategory, DateTime ExpenseDate, bool IsVoided);

public sealed record FundingBucketDetailResponse(
    int Id, string Code, string Name, string FundCode, BucketType BucketType, decimal MaxAdminRate,
    decimal AllocatedAmount, decimal ExpensedAmount, decimal Remaining,
    List<BucketAllocationResponse> Allocations, List<BucketExpenseResponse> Expenses);

public sealed class GetFundingBucketByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/funding-buckets/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var bucket = await db.FundingBuckets
                .Include(b => b.Allocations)
                .Include(b => b.Expenses)
                .FirstOrDefaultAsync(b => b.Id == id, ct);

            if (bucket is null)
            {
                return Results.Problem(title: "FundingBuckets.NotFound", detail: "Funding bucket not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var response = new FundingBucketDetailResponse(
                bucket.Id, bucket.Code, bucket.Name, bucket.FundCode, bucket.BucketType, bucket.MaxAdminRate,
                bucket.AllocatedAmount, bucket.ExpensedAmount, bucket.Remaining,
                bucket.Allocations
                    .Select(a => new BucketAllocationResponse(a.DonationId, a.AllocationType, a.AllocatedAmountPhp, a.AllocationDate))
                    .ToList(),
                bucket.Expenses
                    .Select(e => new BucketExpenseResponse(e.Id, e.AmountPhp, e.ExpenseCategory, e.ExpenseDate, e.IsVoided))
                    .ToList());

            return Results.Ok(response);
        })
        .WithName("GetFundingBucketById")
        .WithTags("FundingBuckets")
        .RequireAuthorization();
    }
}
