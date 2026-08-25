namespace philcare.Api.Features.Programs.Distributions.CreateDistribution;

/// <summary>
/// No BeneficiaryId: a distribution is created empty and beneficiaries are added afterwards via
/// POST /api/distributions/{id}/beneficiaries. No BeneficiaryCount either — reach is derived from
/// that roster, never claimed by the client.
/// </summary>
public sealed record CreateDistributionRequest(
    string DistributionType,
    int ActivityId,
    string FundingBucketCode,
    int Quantity,
    decimal UnitValuePhp,
    DateTime DistributionDate,
    string? Location,
    bool FieldVerified,
    bool ReceivedConfirmation,
    string? ProcessedBy,
    string? PaymentMethod,
    /// <summary>
    /// Required when the funding bucket is a zakat program bucket. It used to be derived from the
    /// single recipient's approved eligibility; with no recipient at creation the officer picks it,
    /// and every beneficiary later added to the roster must match it.
    /// </summary>
    string? ZakatAsnaf,
    string? Notes);

public sealed record CreateDistributionResponse(
    int Id,
    string DistributionType,
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
