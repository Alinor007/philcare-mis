using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Volunteers.UpdateVolunteer;

public sealed record UpdateVolunteerRequest(
    string FullName,
    Gender Gender,
    string? Phone,
    string? Email,
    string? Barangay,
    string? City,
    string? Province,
    string? Region,
    string? Skills,
    string Status,
    bool OrientationCompleted,
    DateTime? OrientationDate,
    bool CodeOfConductSigned,
    DateTime? CodeOfConductDate,
    bool PoliceClearanceOnFile,
    string? Notes,
    bool IsActive);

public sealed record UpdateVolunteerResponse(
    int Id, string FullName, Gender Gender, string Status, bool OrientationCompleted, bool IsActive);
