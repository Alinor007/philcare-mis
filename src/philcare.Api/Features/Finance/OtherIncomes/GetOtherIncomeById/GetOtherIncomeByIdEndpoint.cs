using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.OtherIncomes.GetOtherIncomeById;

public sealed record OtherIncomeAllocationLineResponse(
    AllocationType AllocationType, string TargetBucketCode, decimal AllocationRate, decimal AllocatedAmountPhp);

public sealed record OtherIncomeDetailResponse(
    int Id,
    string IncomeType,
    string Source,
    decimal AmountOriginal,
    string Currency,
    decimal FxRateToPhp,
    decimal AmountPhp,
    DateTime DateReceived,
    string FundCode,
    string FundingBucketCode,
    string? ReceiptNo,
    string? EvidenceLink,
    string? Notes,
    bool IsVoided,
    List<OtherIncomeAllocationLineResponse> Allocations);

public sealed class GetOtherIncomeByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/other-income/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var income = await db.OtherIncomes
                .Include(i => i.Allocations)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (income is null)
            {
                return Results.Problem(title: "OtherIncome.NotFound", detail: "Other income record not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var response = new OtherIncomeDetailResponse(
                income.Id, income.IncomeType, income.Source, income.AmountOriginal, income.Currency, income.FxRateToPhp,
                income.AmountPhp, income.DateReceived, income.FundCode, income.FundingBucketCode, income.ReceiptNo,
                income.EvidenceLink, income.Notes, income.IsVoided,
                income.Allocations
                    .Select(a => new OtherIncomeAllocationLineResponse(a.AllocationType, a.TargetBucketCode, a.AllocationRate, a.AllocatedAmountPhp))
                    .ToList());

            return Results.Ok(response);
        })
        .WithName("GetOtherIncomeById")
        .WithTags("OtherIncome")
        .RequireAuthorization();
    }
}
