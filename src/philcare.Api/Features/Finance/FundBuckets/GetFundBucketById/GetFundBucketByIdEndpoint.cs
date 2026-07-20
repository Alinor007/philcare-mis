using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.FundBuckets.GetFundBucketById;

public sealed record BucketDonationResponse(int Id, int DonorId, decimal Amount, DateTime ReceivedDate, bool IsVoided);

public sealed record BucketExpenseResponse(int Id, decimal Amount, string ExpenseCategory, DateTime ExpenseDate, bool IsVoided);

public sealed record FundBucketDetailResponse(
    int Id, string Name, string FundType, decimal TotalReceived, decimal AdminAllocated,
    decimal ProgramAllocated, decimal TotalExpensed, decimal Balance,
    List<BucketDonationResponse> Donations, List<BucketExpenseResponse> Expenses);

public sealed class GetFundBucketByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/fund-buckets/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var bucket = await db.FundBuckets
                .Include(b => b.Allocations).ThenInclude(a => a.Donation)
                .Include(b => b.Expenses)
                .FirstOrDefaultAsync(b => b.Id == id, ct);

            if (bucket is null)
            {
                return Results.Problem(title: "FundBuckets.NotFound", detail: "Fund bucket not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var response = new FundBucketDetailResponse(
                bucket.Id, bucket.Name, bucket.FundType, bucket.TotalReceived, bucket.AdminAllocated,
                bucket.ProgramAllocated, bucket.TotalExpensed, bucket.Balance,
                bucket.Allocations
                    .Select(a => new BucketDonationResponse(a.Donation.Id, a.Donation.DonorId, a.Donation.Amount, a.Donation.ReceivedDate, a.Donation.IsVoided))
                    .ToList(),
                bucket.Expenses
                    .Select(e => new BucketExpenseResponse(e.Id, e.Amount, e.ExpenseCategory, e.ExpenseDate, e.IsVoided))
                    .ToList());

            return Results.Ok(response);
        })
        .WithName("GetFundBucketById")
        .WithTags("FundBuckets")
        .RequireAuthorization();
    }
}
