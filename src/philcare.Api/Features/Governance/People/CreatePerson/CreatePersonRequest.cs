using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Governance.People.CreatePerson;

public sealed record CreatePersonRequest(
    string FullName,
    string PersonCategory,
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
    /// <summary>
    /// Set by the client only after an officer has been shown a possible-duplicate warning and
    /// chosen to register anyway. Distinct people genuinely share names here, so this can never be
    /// a hard constraint — the override has to exist.
    /// </summary>
    bool ConfirmDuplicate = false);

public sealed record CreatePersonResponse(int Id, string FullName, string PersonCategory, string Status, bool IsActive);
