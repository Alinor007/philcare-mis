using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Beneficiaries.CreateBeneficiary;

public sealed record CreateBeneficiaryRequest(
    string FullName,
    string BeneficiaryType,
    Gender Gender,
    string? Phone,
    string? Barangay,
    string? City,
    string? Province,
    string? Region,
    string? Country,
    string? VulnerabilityCategory,
    string? SafeguardingCategory,
    bool ConsentOnFile,
    string? Remarks,
    string? PhotoUrl,
    /// <summary>
    /// Set by the client only after an officer has been shown a possible-duplicate warning and
    /// chosen to register anyway. Two real people genuinely share a name here, so this can never
    /// be a hard constraint — the override has to exist.
    /// </summary>
    bool ConfirmDuplicate = false);

public sealed record CreateBeneficiaryResponse(
    int Id,
    string FullName,
    string BeneficiaryType,
    Gender Gender,
    string? VulnerabilityCategory,
    string? SafeguardingCategory,
    bool ConsentOnFile,
    string Status,
    bool IsActive,
    bool SafeguardingWarning,
    string? SafeguardingMessage);
