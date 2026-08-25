using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Beneficiaries.UpdateBeneficiary;

public sealed record UpdateBeneficiaryRequest(
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
    string Status,
    string? Remarks,
    string? PhotoUrl,
    bool IsActive);

public sealed record UpdateBeneficiaryResponse(
    int Id, string FullName, string BeneficiaryType, Gender Gender, string Status, bool IsActive,
    bool SafeguardingWarning, string? SafeguardingMessage);
