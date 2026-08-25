namespace philcare.Api.Features.Programs.DistributionBeneficiaries.UpdateDistributionBeneficiary;

public sealed record UpdateDistributionBeneficiaryRequest(
    bool ReceivedConfirmation, string? EvidenceLink, string? Remarks);

public sealed record UpdateDistributionBeneficiaryResponse(
    int Id, int DistributionId, int BeneficiaryId, string BeneficiaryName, bool ReceivedConfirmation);
