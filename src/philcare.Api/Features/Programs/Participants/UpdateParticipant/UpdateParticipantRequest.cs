using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Participants.UpdateParticipant;

public sealed record UpdateParticipantRequest(
    string FullName,
    string ParticipantType,
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
    string Status,
    string? Remarks,
    bool IsActive);

public sealed record UpdateParticipantResponse(
    int Id, string FullName, string ParticipantType, Gender Gender, string Status, bool IsActive);
