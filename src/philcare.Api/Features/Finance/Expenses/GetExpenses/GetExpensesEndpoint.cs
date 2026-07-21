using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Expenses.GetExpenses;

public sealed record ExpenseListItemResponse(
    int Id, string FundingBucketCode, decimal AmountPhp, string ExpenseCategory, DateTime ExpenseDate, bool IsVoided);

public sealed class GetExpensesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/expenses", async (
            string? fundingBucketCode,
            string? expenseCategory,
            DateTime? from,
            DateTime? to,
            bool? includeVoided,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var query = db.Expenses.AsQueryable();

            if (includeVoided != true)
            {
                query = query.Where(e => !e.IsVoided);
            }

            if (!string.IsNullOrWhiteSpace(fundingBucketCode))
            {
                query = query.Where(e => e.FundingBucketCode == fundingBucketCode);
            }

            if (!string.IsNullOrWhiteSpace(expenseCategory))
            {
                query = query.Where(e => e.ExpenseCategory == expenseCategory);
            }

            if (from is not null)
            {
                query = query.Where(e => e.ExpenseDate >= from);
            }

            if (to is not null)
            {
                query = query.Where(e => e.ExpenseDate <= to);
            }

            var expenses = await query
                .OrderByDescending(e => e.ExpenseDate)
                .Select(e => new ExpenseListItemResponse(e.Id, e.FundingBucketCode, e.AmountPhp, e.ExpenseCategory, e.ExpenseDate, e.IsVoided))
                .ToListAsync(ct);

            return Results.Ok(expenses);
        })
        .WithName("GetExpenses")
        .WithTags("Expenses")
        .RequireAuthorization();
    }
}
