using philcare.Api.Common.Domain;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.HumanResources.Domain;

/// <summary>
/// Paid staff roster — the org's employee directory.
///
/// Distinct from <see cref="Volunteer"/> (unpaid, and safeguarding-gated before it can be put on
/// an activity) and from Governance.Person (board/committee identity, whose roster is derived
/// from Assignments). A person can legitimately be more than one of the three; the records are
/// not linked today, which is a known gap rather than an oversight.
///
/// No foreign key to User: not every staff member has a system login, and logins outlive
/// employment. Staff attribution on records continues to come from the audit fields on
/// <see cref="Entity"/> (CreatedBy/UpdatedBy), same as DonorEngagement.
/// </summary>
public class StaffMember : Entity
{
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;

    // Reuses the existing org-unit vocabulary rather than a new category — the same list backs
    // AidProgram.OwnerDepartment and mirrors the OrgBody chart.
    public string? Department { get; set; } // lookup: owner_department

    public string EmploymentType { get; set; } = string.Empty; // lookup: employment_type

    // Nullable: the org's existing staff sheet has rows with no recorded start date, and a
    // non-nullable DateTime would import those as 0001-01-01, which MariaDB datetime(6) rejects.
    public DateTime? HiredDate { get; set; }

    public string? Email { get; set; }
    public string? Phone { get; set; }
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
