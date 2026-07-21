namespace philcare.Api.Features.Finance.Expenses.CreateExpense;

public sealed record CreateExpenseRequest(
    DateTime ExpenseDate,
    string PayeeVendor,
    string ExpenseCategory,
    string Description,
    string? ProgramOrProject,
    string PaymentMethod,
    decimal AmountOriginal,
    string Currency,
    decimal FxRateToPhp,
    string? ReceiptNo,
    string? ApprovedBy,
    string? SupportingDocStatus,
    int? LinkedDonationId,
    string? ExpenseFunction,
    string FundingBucketCode,
    string? ZakatAsnaf,
    int? BeneficiaryCount,
    string? BeneficiaryType,
    string? Notes);

public sealed record CreateExpenseResponse(
    int Id,
    string FundCode,
    string FundingBucketCode,
    decimal AmountOriginal,
    string Currency,
    decimal FxRateToPhp,
    decimal AmountPhp,
    string ExpenseCategory,
    DateTime ExpenseDate,
    string Description,
    string ApprovalStatus,
    string? ZakatAsnaf,
    int? BeneficiaryCount,
    bool IsVoided,
    decimal RemainingBucketBalance);
