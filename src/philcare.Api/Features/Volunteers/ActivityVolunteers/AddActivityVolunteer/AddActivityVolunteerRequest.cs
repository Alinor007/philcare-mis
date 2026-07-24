namespace philcare.Api.Features.Volunteers.ActivityVolunteers.AddActivityVolunteer;

public sealed record AddActivityVolunteerRequest(
    int VolunteerId, string? RoleInActivity, string? AttendanceStatus, decimal? HoursServed, string? Remarks);

public sealed record AddActivityVolunteerResponse(
    int Id, int ActivityId, int VolunteerId, string VolunteerName, string? RoleInActivity, string? AttendanceStatus);
