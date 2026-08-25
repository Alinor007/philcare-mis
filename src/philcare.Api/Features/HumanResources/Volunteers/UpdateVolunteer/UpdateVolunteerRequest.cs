namespace philcare.Api.Features.HumanResources.Volunteers.UpdateVolunteer;

/// <summary>No PersonId — a volunteer profile stays attached to the Person it was created for.</summary>
public sealed record UpdateVolunteerRequest(
    string? Skills,
    string? AvailabilityDays,
    string Status,
    bool OrientationCompleted,
    DateTime? OrientationDate,
    bool CodeOfConductSigned,
    DateTime? CodeOfConductDate,
    bool PoliceClearanceOnFile,
    string? Notes,
    bool IsActive);

public sealed record UpdateVolunteerResponse(
    int Id, int PersonId, string FullName, string Status, bool OrientationCompleted, bool IsActive);
