using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Expenses.CreateExpense;

public sealed class CreateExpenseHandler(AppDbContext db)
{
    public async Task<Result<CreateExpenseResponse>> HandleAsync(CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        var bucket = await db.FundBuckets.FirstOrDefaultAsync(b => b.Id == request.FundBucketId, cancellationToken);

        if (bucket is null)
        {
            return Result.Failure<CreateExpenseResponse>(Error.NotFound("Expenses.FundBucketNotFound", "Fund bucket not found."));
        }

        if (bucket.Balance < request.Amount)
        {
            return Result.Failure<CreateExpenseResponse>(
                Error.Validation("Expenses.InsufficientBalance", "The fund bucket does not have enough balance to cover this expense."));
        }

        var isZakat = string.Equals(bucket.FundType, FinanceRules.ZakatFundType, StringComparison.OrdinalIgnoreCase);

        if (isZakat)
        {
            if (string.IsNullOrWhiteSpace(request.ZakatAsnaf))
            {
                return Result.Failure<CreateExpenseResponse>(
                    Error.Validation("Expenses.ZakatAsnafRequired", "Zakat asnaf is required for expenses against a zakat fund bucket."));
            }

            if (request.BeneficiaryCount is null or 0)
            {
                return Result.Failure<CreateExpenseResponse>(
                    Error.Validation("Expenses.BeneficiaryCountRequired", "Beneficiary count is required for expenses against a zakat fund bucket."));
            }
        }

        var expense = new Expense
        {
            FundBucketId = bucket.Id,
            Amount = request.Amount,
            ExpenseCategory = request.ExpenseCategory,
            PaymentMethod = request.PaymentMethod,
            ExpenseDate = request.ExpenseDate,
            Description = request.Description,
            Reference = request.Reference,
            ZakatAsnaf = request.ZakatAsnaf,
            BeneficiaryCount = request.BeneficiaryCount,
            IsVoided = false
        };

        db.Expenses.Add(expense);

        bucket.TotalExpensed += request.Amount;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateExpenseResponse(
            expense.Id, expense.FundBucketId, expense.Amount, expense.ExpenseCategory, expense.PaymentMethod,
            expense.ExpenseDate, expense.Description, expense.Reference, expense.ZakatAsnaf, expense.BeneficiaryCount,
            expense.IsVoided, bucket.Balance));
    }
}
