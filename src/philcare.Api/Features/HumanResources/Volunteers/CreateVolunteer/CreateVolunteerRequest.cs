namespace philcare.Api.Features.HumanResources.Volunteers.CreateVolunteer;

/// <summary>
/// No FullName/Gender/contact/address/PhotoUrl — those live on the Person this profile is attached
/// to. The coordinator picks (or creates) a Person first, then fills in these volunteering fields.
/// </summary>
public sealed record CreateVolunteerRequest(
    int PersonId,
    string? Skills,
    string? AvailabilityDays,
    bool OrientationCompleted,
    DateTime? OrientationDate,
    bool CodeOfConductSigned,
    DateTime? CodeOfConductDate,
    bool PoliceClearanceOnFile,
    string? Notes);

public sealed record CreateVolunteerResponse(
    int Id, int PersonId, string FullName, string Status, bool OrientationCompleted, bool IsActive);
