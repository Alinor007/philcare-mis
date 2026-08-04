using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Participants.CreateParticipant;

public sealed record CreateParticipantRequest(
    string FullName,
    string ParticipantType,
    string BeneficiaryType,
    Gender Gender,
    string? AgeGroup,
    string? Phone,
    string? Barangay,
    string? City,
    string? Province,
    string? Region,
    string? Country,
    string? VulnerabilityCategory,
    string? SafeguardingCategory,
    bool ConsentOnFile,
    string? Remarks);

public sealed record CreateParticipantResponse(
    int Id,
    string FullName,
    string ParticipantType,
    string BeneficiaryType,
    Gender Gender,
    string? VulnerabilityCategory,
    string? SafeguardingCategory,
    bool ConsentOnFile,
    string Status,
    bool IsActive);
