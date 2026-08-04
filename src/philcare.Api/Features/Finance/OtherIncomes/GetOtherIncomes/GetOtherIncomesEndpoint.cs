using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.OtherIncomes.GetOtherIncomes;

public sealed record OtherIncomeListItemResponse(
    int Id, string IncomeType, string Source, decimal AmountPhp, string FundingBucketCode, DateTime DateReceived, bool IsVoided);

public sealed class GetOtherIncomesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/other-income", async (
            string? incomeType,
            string? fundingBucketCode,
            DateTime? from,
            DateTime? to,
            bool? includeVoided,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var query = db.OtherIncomes.AsQueryable();

            if (includeVoided != true)
            {
                query = query.Where(i => !i.IsVoided);
            }

            if (!string.IsNullOrWhiteSpace(incomeType))
            {
                query = query.Where(i => i.IncomeType == incomeType);
            }

            if (!string.IsNullOrWhiteSpace(fundingBucketCode))
            {
                query = query.Where(i => i.FundingBucketCode == fundingBucketCode);
            }

            if (from is not null)
            {
                query = query.Where(i => i.DateReceived >= from);
            }

            if (to is not null)
            {
                query = query.Where(i => i.DateReceived <= to);
            }

            var incomes = await query
                .OrderByDescending(i => i.DateReceived)
                .Select(i => new OtherIncomeListItemResponse(i.Id, i.IncomeType, i.Source, i.AmountPhp, i.FundingBucketCode, i.DateReceived, i.IsVoided))
                .ToListAsync(ct);

            return Results.Ok(incomes);
        })
        .WithName("GetOtherIncomes")
        .WithTags("OtherIncome")
        .RequireAuthorization();
    }
}
