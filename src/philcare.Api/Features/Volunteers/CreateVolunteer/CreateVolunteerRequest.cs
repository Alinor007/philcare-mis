using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Volunteers.CreateVolunteer;

public sealed record CreateVolunteerRequest(
    string FullName,
    Gender Gender,
    string? Phone,
    string? Email,
    string? Barangay,
    string? City,
    string? Province,
    string? Region,
    string? Skills,
    bool OrientationCompleted,
    DateTime? OrientationDate,
    bool CodeOfConductSigned,
    DateTime? CodeOfConductDate,
    bool PoliceClearanceOnFile,
    string? Notes);

public sealed record CreateVolunteerResponse(
    int Id, string FullName, Gender Gender, string Status, bool OrientationCompleted, bool IsActive);
