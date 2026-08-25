namespace philcare.Api.Features.Programs.DistributionBeneficiaries.AddDistributionBeneficiary;

public sealed record AddDistributionBeneficiaryRequest(
    int BeneficiaryId,
    bool ReceivedConfirmation,
    string? EvidenceLink,
    string? Remarks,
    /// <summary>
    /// Set by the client only after an officer has been shown a possible-duplicate warning and
    /// chosen to record anyway — the same person can legitimately be issued the same aid twice in
    /// one day, so this is a warning gate rather than a hard block.
    /// </summary>
    bool ConfirmDuplicate = false);

/// <summary>
/// <c>BeneficiaryCount</c> is the distribution's recomputed reach, returned so the caller can
/// update "people reached" without a follow-up fetch.
/// </summary>
public sealed record AddDistributionBeneficiaryResponse(
    int Id,
    int DistributionId,
    int BeneficiaryId,
    string BeneficiaryName,
    bool ReceivedConfirmation,
    int BeneficiaryCount);
