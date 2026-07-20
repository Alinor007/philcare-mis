using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Expenses.GetExpenses;

public sealed record ExpenseListItemResponse(
    int Id, int FundBucketId, decimal Amount, string ExpenseCategory, DateTime ExpenseDate, bool IsVoided);

public sealed class GetExpensesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/expenses", async (
            int? fundBucketId,
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

            if (fundBucketId is not null)
            {
                query = query.Where(e => e.FundBucketId == fundBucketId);
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
                .Select(e => new ExpenseListItemResponse(e.Id, e.FundBucketId, e.Amount, e.ExpenseCategory, e.ExpenseDate, e.IsVoided))
                .ToListAsync(ct);

            return Results.Ok(expenses);
        })
        .WithName("GetExpenses")
        .WithTags("Expenses")
        .RequireAuthorization();
    }
}
