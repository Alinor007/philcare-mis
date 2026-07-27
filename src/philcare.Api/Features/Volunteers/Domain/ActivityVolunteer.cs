using philcare.Api.Common.Domain;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Volunteers.Domain;

/// <summary>Enrollment join between an Activity and a Volunteer — mirrors ActivityParticipant.</summary>
public class ActivityVolunteer : Entity
{
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;

    public int VolunteerId { get; set; }
    public Volunteer Volunteer { get; set; } = null!;

    public string? RoleInActivity { get; set; }
    public string? AttendanceStatus { get; set; }
    public decimal? HoursServed { get; set; }
    public string? Remarks { get; set; }
}
