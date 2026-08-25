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

        if (request.ApprovedByPersonId is not null)
        {
            var approverExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.ApprovedByPersonId, cancellationToken);

            if (!approverExists)
            {
                return Result.Failure<CreateExpenseResponse>(Error.NotFound("Expenses.ApprovedByPersonNotFound", "Approver not found."));
            }
        }

        var posting = ExpensePosting.Post(bucket, new ExpensePostingRequest(
            request.ExpenseDate, request.PayeeVendor, request.ExpenseCategory, request.Description, request.PaymentMethod,
            request.AmountOriginal, request.Currency, request.FxRateToPhp, request.ProgramOrProject, request.ReceiptNo,
            request.ApprovedByPersonId, request.SupportingDocStatus, request.LinkedDonationId, request.ExpenseFunction,
            request.ZakatAsnaf, request.BeneficiaryCount, request.BeneficiaryType, request.Notes));

        if (posting.IsFailure)
        {
            return Result.Failure<CreateExpenseResponse>(posting.Error);
        }

        var expense = posting.Value;

        db.Expenses.Add(expense);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateExpenseResponse(
            expense.Id, expense.FundCode, expense.FundingBucketCode, expense.AmountOriginal, expense.Currency, expense.FxRateToPhp,
            expense.AmountPhp, expense.ExpenseCategory, expense.ExpenseDate, expense.Description, expense.ApprovalStatus,
            expense.ZakatAsnaf, expense.BeneficiaryCount, expense.IsVoided, bucket.Remaining));
    }
}
