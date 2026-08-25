using philcare.Api.Common.Domain;
using philcare.Api.Features.People.Domain;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.HumanResources.Domain;

/// <summary>
/// Volunteer role profile — the unpaid-service fields for a <see cref="Person"/>. Identity
/// (name, gender, contact, address, photo) lives on Person; at most one Volunteer row exists per
/// Person (enforced by a unique index on PersonId).
///
/// Tracks safeguarding-orientation compliance, which gates enrollment into safeguarding-risk
/// activities — see AddActivityVolunteerHandler. Distinct from Beneficiary (aid recipient, out of
/// scope for Person unification) and from <see cref="StaffMember"/> (paid). One Person can now
/// legitimately hold both a volunteer and a staff profile, which the old separate-tables model
/// could not represent.
/// </summary>
public class Volunteer : Entity
{
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public string? Skills { get; set; }

    // Free text on purpose — "Weekends", "Wed/Fri evenings", "school holidays only". Coordinators
    // write what the volunteer told them; a structured schedule would need a real availability
    // model, which nothing currently asks for.
    public string? AvailabilityDays { get; set; }

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
