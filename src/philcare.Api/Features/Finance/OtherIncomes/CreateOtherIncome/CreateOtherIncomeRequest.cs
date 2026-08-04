namespace philcare.Api.Features.Finance.OtherIncomes.CreateOtherIncome;

public sealed record CreateOtherIncomeRequest(
    string IncomeType,
    string Source,
    DateTime DateReceived,
    decimal AmountOriginal,
    string Currency,
    decimal FxRateToPhp,
    string FundingBucketCode,
    string? ReceiptNo,
    string? EvidenceLink,
    string? Notes);

public sealed record CreateOtherIncomeResponse(
    int Id,
    string IncomeType,
    string Source,
    decimal AmountOriginal,
    string Currency,
    decimal FxRateToPhp,
    decimal AmountPhp,
    DateTime DateReceived,
    string FundCode,
    string FundingBucketCode,
    string? ReceiptNo,
    string? EvidenceLink,
    string? Notes,
    bool IsVoided);
