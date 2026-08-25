using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

/// <summary>
/// Fields needed to post a new Expense against an already-loaded, tracked FundingBucket.
/// FundCode and FundingBucketCode are deliberately not here — they are always derived from the
/// bucket the caller already loaded, never re-specified by the caller.
/// </summary>
public sealed record ExpensePostingRequest(
    DateTime ExpenseDate,
    string PayeeVendor,
    string ExpenseCategory,
    string Description,
    string PaymentMethod,
    decimal AmountOriginal,
    string Currency,
    decimal FxRateToPhp,
    string? ProgramOrProject = null,
    string? ReceiptNo = null,
    int? ApprovedByPersonId = null,
    string? SupportingDocStatus = null,
    int? LinkedDonationId = null,
    string? ExpenseFunction = null,
    string? ZakatAsnaf = null,
    int? BeneficiaryCount = null,
    string? BeneficiaryType = null,
    string? Notes = null);

/// <summary>
/// Shared expense-posting logic, extracted from CreateExpenseHandler / VoidExpenseHandler so
/// Distributions can post into the Finance ledger through the same rules Expense already
/// enforces, without a second money-mover. Mirrors the FinanceRules idiom (a static class of
/// Finance policy, not a handler) — there is no Services/ folder in this codebase and this
/// isn't one either.
///
/// This never touches AppDbContext and never calls SaveChangesAsync: both call sites already
/// load the bucket themselves, and handing back an unsaved Expense (with the bucket already
/// debited in memory) is what lets a caller like CreateDistributionHandler write both rows in
/// a single SaveChangesAsync.
/// </summary>
public static class ExpensePosting
{
    public static bool IsZakatProgramBucket(FundingBucket bucket) =>
        string.Equals(bucket.FundCode, FinanceRules.ZakatFundCode, StringComparison.OrdinalIgnoreCase)
        && bucket.BucketType == BucketType.Program;

    /// <summary>
    /// Checks balance + zakat rules, builds the Expense, and DEBITS the bucket in-memory. The
    /// debit is inseparable from the check — that is the whole point of this method. Caller owns
    /// Add() and SaveChangesAsync(). Call this LAST, after all other validation: returning a
    /// failure after a successful Post() leaves the tracked bucket dirty for the rest of the request.
    /// </summary>
    public static Result<Expense> Post(FundingBucket bucket, ExpensePostingRequest request)
    {
        var amountPhp = Math.Round(request.AmountOriginal * request.FxRateToPhp, 2);

        if (bucket.Remaining < amountPhp)
        {
            return Result.Failure<Expense>(
                Error.Validation("Expenses.InsufficientBalance", "The funding bucket does not have enough balance to cover this expense."));
        }

        if (IsZakatProgramBucket(bucket))
        {
            if (string.IsNullOrWhiteSpace(request.ZakatAsnaf))
            {
                return Result.Failure<Expense>(
                    Error.Validation("Expenses.ZakatAsnafRequired", "Zakat asnaf is required for expenses against the zakat program bucket."));
            }

            // No BeneficiaryCount requirement. A distribution is now booked before anyone has been
            // recorded as receiving it, so reach is legitimately 0 at posting time and is corrected
            // by DistributionReach.Sync as the roster fills. Asnaf above is still required, because
            // it is chosen up front and every roster member must match it.
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
            ApprovedByPersonId = request.ApprovedByPersonId,
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

        bucket.ExpensedAmount += amountPhp;

        return Result.Success(expense);
    }

    /// <summary>Reverses a posted expense. No-op on the bucket if already voided.</summary>
    public static void Reverse(Expense expense, FundingBucket bucket)
    {
        if (expense.IsVoided)
        {
            return;
        }

        expense.IsVoided = true;
        bucket.ExpensedAmount -= expense.AmountPhp;
    }
}
