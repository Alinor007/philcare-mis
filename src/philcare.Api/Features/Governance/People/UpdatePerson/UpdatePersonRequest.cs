using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Governance.People.UpdatePerson;

public sealed record UpdatePersonRequest(
    string FullName,
    string PersonCategory,
    string Status,
    string? Email,
    string? ContactNumber,
    string? DateOfBirth,
    Gender Gender,
    string? CivilStatus,
    string? Barangay,
    string? City,
    string? Province,
    string? Region,
    string? EmergencyContactName,
    string? EmergencyContactNumber,
    string? PhotoUrl,
    bool DefaultVotingRights,
    string? Notes,
    bool IsActive);

public sealed record UpdatePersonResponse(int Id, string FullName, string PersonCategory, string Status, bool IsActive);
