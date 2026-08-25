using philcare.Api.Common.Domain;
using philcare.Api.Features.People.Domain;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.HumanResources.Domain;

/// <summary>
/// Paid-staff role profile — employment-specific fields for a <see cref="Person"/>. Identity
/// (name, contact, photo) lives on Person; at most one StaffMember row exists per Person
/// (enforced by a unique index on PersonId).
///
/// Distinct from <see cref="Volunteer"/> (unpaid, and safeguarding-gated before it can be put on
/// an activity) and from Governance's Assignment (board/committee role, granted independently of
/// employment). A Person can legitimately hold any combination of the three now that they share
/// one identity record — this used to be "a known gap rather than an oversight"; Person
/// unification is that gap closed.
///
/// No foreign key to User: not every staff member has a system login, and logins outlive
/// employment. Staff attribution on records continues to come from the audit fields on
/// <see cref="Entity"/> (CreatedBy/UpdatedBy), same as DonorEngagement.
/// </summary>
public class StaffMember : Entity
{
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public string Position { get; set; } = string.Empty;

    // Reuses the existing org-unit vocabulary rather than a new category — the same list backs
    // AidProgram.OwnerDepartment and mirrors the OrgBody chart.
    public string? Department { get; set; } // lookup: owner_department

    public string EmploymentType { get; set; } = string.Empty; // lookup: employment_type

    // Nullable: the org's existing staff sheet has rows with no recorded start date, and a
    // non-nullable DateTime would import those as 0001-01-01, which MariaDB datetime(6) rejects.
    public DateTime? HiredDate { get; set; }

    // Reporting line. References Person, not another StaffMember row — a supervisor doesn't need
    // their own employment profile in this system to be named as one (e.g. an external advisor,
    // or someone whose StaffMember record hasn't been entered yet). Loosely enforced: the handler
    // checks the Person exists, but nothing stops a supervisor who is later deactivated or whose
    // own StaffMember profile is removed.
    public int? SupervisorPersonId { get; set; }
    public Person? SupervisorPerson { get; set; }

    public string? Notes { get; set; }

    // Soft-deactivation only, per the repo-wide rule. Deliberately the *only* lifecycle field:
    // Volunteer carries both Status and IsActive and its handler hardcodes Status, so the lookup
    // there is decorative. A nullable SeparationDate is the right addition if departures ever
    // need dating.
    public bool IsActive { get; set; } = true;

    // Activities this staff member is rostered to run. The join is still named
    // ActivityParticipant for table-compatibility reasons — see that class.
    public List<ActivityParticipant> ActivityParticipants { get; set; } = [];
}
