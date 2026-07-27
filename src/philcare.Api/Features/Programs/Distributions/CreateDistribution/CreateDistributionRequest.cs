namespace philcare.Api.Features.Programs.Distributions.CreateDistribution;

public sealed record CreateDistributionRequest(
    string DistributionType,
    int ParticipantId,
    int? ActivityId,
    string? FundingBucketCode,
    int Quantity,
    decimal TotalValuePhp,
    DateTime DistributionDate,
    string? Location,
    bool FieldVerified,
    bool ReceivedConfirmation,
    string? ProcessedBy,
    string? ZakatAsnaf,
    string? Notes);

public sealed record CreateDistributionResponse(
    int Id,
    string DistributionType,
    int ParticipantId,
    int? ActivityId,
    string? FundingBucketCode,
    int Quantity,
    decimal TotalValuePhp,
    DateTime DistributionDate,
    string? ZakatAsnaf,
    bool IsVoided);
