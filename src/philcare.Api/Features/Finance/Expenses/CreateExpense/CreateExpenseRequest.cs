namespace philcare.Api.Features.Finance.Expenses.CreateExpense;

public sealed record CreateExpenseRequest(
    int FundBucketId,
    decimal Amount,
    string ExpenseCategory,
    string PaymentMethod,
    DateTime ExpenseDate,
    string Description,
    string? Reference,
    string? ZakatAsnaf,
    int? BeneficiaryCount);

public sealed record CreateExpenseResponse(
    int Id,
    int FundBucketId,
    decimal Amount,
    string ExpenseCategory,
    string PaymentMethod,
    DateTime ExpenseDate,
    string Description,
    string? Reference,
    string? ZakatAsnaf,
    int? BeneficiaryCount,
    bool IsVoided,
    decimal RemainingBucketBalance);
