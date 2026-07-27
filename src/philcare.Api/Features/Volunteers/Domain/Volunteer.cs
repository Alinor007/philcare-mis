using philcare.Api.Common.Domain;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Volunteers.Domain;

/// <summary>
/// Volunteer registry, separate from Participant (which is beneficiary-centric). Tracks
/// safeguarding-orientation compliance, which gates enrollment into safeguarding-risk activities.
/// </summary>
public class Volunteer : Entity
{
    public string FullName { get; set; } = string.Empty;
    public Gender Gender { get; set; } = Gender.Unspecified;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public string? Barangay { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Region { get; set; }

    public string? Skills { get; set; }
    public string Status { get; set; } = "ACTIVE"; // lookup: volunteer_status

    public bool OrientationCompleted { get; set; }
    public DateTime? OrientationDate { get; set; }
    public bool CodeOfConductSigned { get; set; }
    public DateTime? CodeOfConductDate { get; set; }
    public bool PoliceClearanceOnFile { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<ActivityVolunteer> ActivityVolunteers { get; set; } = [];
}
