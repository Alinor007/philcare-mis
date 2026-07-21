using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Expenses.GetExpenseById;

public sealed record ExpenseDetailResponse(
    int Id,
    string FundCode,
    string FundingBucketCode,
    decimal AmountOriginal,
    string Currency,
    decimal FxRateToPhp,
    decimal AmountPhp,
    string ExpenseCategory,
    string PaymentMethod,
    DateTime ExpenseDate,
    string Description,
    string? ReceiptNo,
    string ApprovalStatus,
    string? ApprovedBy,
    string? ZakatAsnaf,
    int? BeneficiaryCount,
    bool IsVoided);

public sealed class GetExpenseByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/expenses/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var expense = await db.Expenses
                .Where(e => e.Id == id)
                .Select(e => new ExpenseDetailResponse(
                    e.Id, e.FundCode, e.FundingBucketCode, e.AmountOriginal, e.Currency, e.FxRateToPhp, e.AmountPhp,
                    e.ExpenseCategory, e.PaymentMethod, e.ExpenseDate, e.Description, e.ReceiptNo, e.ApprovalStatus,
                    e.ApprovedBy, e.ZakatAsnaf, e.BeneficiaryCount, e.IsVoided))
                .FirstOrDefaultAsync(ct);

            if (expense is null)
            {
                return Results.Problem(title: "Expenses.NotFound", detail: "Expense not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(expense);
        })
        .WithName("GetExpenseById")
        .WithTags("Expenses")
        .RequireAuthorization();
    }
}
