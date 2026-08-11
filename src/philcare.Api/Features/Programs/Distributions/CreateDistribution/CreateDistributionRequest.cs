namespace philcare.Api.Features.Programs.Distributions.CreateDistribution;

public sealed record CreateDistributionRequest(
    string DistributionType,
    int BeneficiaryId,
    int ActivityId,
    string FundingBucketCode,
    int Quantity,
    decimal UnitValuePhp,
    int BeneficiaryCount,
    DateTime DistributionDate,
    string? Location,
    bool FieldVerified,
    bool ReceivedConfirmation,
    string? ProcessedBy,
    string? PaymentMethod,
    string? ZakatAsnaf,
    string? Notes);

public sealed record CreateDistributionResponse(
    int Id,
    string DistributionType,
    int BeneficiaryId,
    int ActivityId,
    string FundingBucketCode,
    int Quantity,
    decimal UnitValuePhp,
    decimal TotalValuePhp,
    int BeneficiaryCount,
    DateTime DistributionDate,
    string? ZakatAsnaf,
    bool IsVoided,
    int? ExpenseId,
    decimal RemainingBucketBalance);
