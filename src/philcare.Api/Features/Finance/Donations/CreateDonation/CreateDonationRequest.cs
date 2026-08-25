using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Donations.CreateDonation;

public sealed record CreateDonationRequest(
    int DonorId,
    decimal AmountOriginal,
    string Currency,
    decimal FxRateToPhp,
    DateTime DateReceived,
    string Channel,
    string? Purpose,
    bool RestrictedFlag,
    string? ProgramOrProject,
    string FundCode,
    string? CashDocumentationStatus,
    bool SourceVerified,
    bool AdminAllowed,
    decimal AdminRateInput,
    string? Notes,
    string? TransactionRef);

public sealed record AllocationLineResponse(
    AllocationType AllocationType, string TargetBucketCode, decimal AllocationRate, decimal AllocatedAmountPhp);

public sealed record CreateDonationResponse(
    int Id,
    int DonorId,
    decimal AmountOriginal,
    string Currency,
    decimal FxRateToPhp,
    decimal AmountPhp,
    DateTime DateReceived,
    string Channel,
    string? Purpose,
    bool RestrictedFlag,
    string? ProgramOrProject,
    string FundCode,
    string? ReceiptNo,
    string? TransactionRef,
    bool AdminAllowed,
    decimal AdminRateInput,
    decimal AdminRateCap,
    decimal AdminRateApplied,
    decimal ProgramAllocationPhp,
    decimal AdminAllocationPhp,
    string? ProgramBucketCode,
    string? AdminBucketCode,
    string AllocationStatus,
    bool IsVoided,
    List<AllocationLineResponse> Allocations);
