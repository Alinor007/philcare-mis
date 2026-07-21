using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Expenses.CreateExpense;

public sealed class CreateExpenseHandler(AppDbContext db)
{
    public async Task<Result<CreateExpenseResponse>> HandleAsync(CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        var bucket = await db.FundingBuckets.FirstOrDefaultAsync(b => b.Code == request.FundingBucketCode, cancellationToken);

        if (bucket is null)
        {
            return Result.Failure<CreateExpenseResponse>(Error.NotFound("Expenses.FundingBucketNotFound", "Funding bucket not found."));
        }

        var amountPhp = Math.Round(request.AmountOriginal * request.FxRateToPhp, 2);

        if (bucket.Remaining < amountPhp)
        {
            return Result.Failure<CreateExpenseResponse>(
                Error.Validation("Expenses.InsufficientBalance", "The funding bucket does not have enough balance to cover this expense."));
        }

        var isZakatProgramBucket = string.Equals(bucket.FundCode, FinanceRules.ZakatFundCode, StringComparison.OrdinalIgnoreCase)
            && bucket.BucketType == BucketType.Program;

        if (isZakatProgramBucket)
        {
            if (string.IsNullOrWhiteSpace(request.ZakatAsnaf))
            {
                return Result.Failure<CreateExpenseResponse>(
                    Error.Validation("Expenses.ZakatAsnafRequired", "Zakat asnaf is required for expenses against the zakat program bucket."));
            }

            if (request.BeneficiaryCount is null or 0)
            {
                return Result.Failure<CreateExpenseResponse>(
                    Error.Validation("Expenses.BeneficiaryCountRequired", "Beneficiary count is required for expenses against the zakat program bucket."));
            }
        }

        var expense = new Expense
        {
            ExpenseDate = request.ExpenseDate,
            PayeeVendor = request.PayeeVendor,
            ExpenseCategory = request.ExpenseCategory,
            Description = request.Description,
            FundCode = bucket.FundCode,
            ProgramOrProject = request.ProgramOrProject,
            PaymentMethod = request.PaymentMethod,
            AmountOriginal = request.AmountOriginal,
            Currency = request.Currency,
            FxRateToPhp = request.FxRateToPhp,
            AmountPhp = amountPhp,
            ReceiptNo = request.ReceiptNo,
            ApprovalStatus = "Approved",
            ApprovedBy = request.ApprovedBy,
            SupportingDocStatus = request.SupportingDocStatus,
            LinkedDonationId = request.LinkedDonationId,
            ExpenseFunction = request.ExpenseFunction,
            FundingBucketCode = bucket.Code,
            ZakatAsnaf = request.ZakatAsnaf,
            BeneficiaryCount = request.BeneficiaryCount,
            BeneficiaryType = request.BeneficiaryType,
            Notes = request.Notes,
            IsVoided = false
        };

        db.Expenses.Add(expense);

        bucket.ExpensedAmount += amountPhp;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateExpenseResponse(
            expense.Id, expense.FundCode, expense.FundingBucketCode, expense.AmountOriginal, expense.Currency, expense.FxRateToPhp,
            expense.AmountPhp, expense.ExpenseCategory, expense.ExpenseDate, expense.Description, expense.ApprovalStatus,
            expense.ZakatAsnaf, expense.BeneficiaryCount, expense.IsVoided, bucket.Remaining));
    }
}
