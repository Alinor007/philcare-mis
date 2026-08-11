using philcare.Api.Common.Domain;
using philcare.Api.Features.HumanResources.Domain;

namespace philcare.Api.Features.Programs.Domain;

/// <summary>
/// Staffing roster for an Activity — which staff members ran it, in what role, and whether they
/// turned up. Sits alongside <see cref="ActivityVolunteer"/> (unpaid helpers); together they
/// answer "who delivered this activity".
///
/// It does NOT record who received the activity. Beneficiary reach is tracked through
/// <see cref="Distribution"/> and the activity's own ActualBeneficiaries closeout figure.
///
/// The class and table keep the "ActivityParticipant(s)" name from when this held beneficiaries;
/// renaming the table is the one operation this MariaDB target cannot do safely.
/// </summary>
public class ActivityParticipant : Entity
{
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;

    public int StaffMemberId { get; set; }
    public StaffMember StaffMember { get; set; } = null!;

    public string? RoleInActivity { get; set; }
    public string? AttendanceStatus { get; set; } // lookup: attendance_status

    // Retained from the beneficiary era. No longer enforced: the consent gate compared this
    // against Beneficiary.ConsentOnFile, which a staff member does not have. Kept as a plain
    // flag rather than dropped, because dropping a column is not an additive migration.
    public bool ConsentRequired { get; set; }

    public string? EvidenceLink { get; set; }
    public string? Remarks { get; set; }

    // Soft delete — removal keeps attendance/audit history. The unique (ActivityId, StaffMemberId)
    // index means a re-assignment after removal reactivates this same row rather than inserting a
    // second one (see AddActivityParticipantHandler); it can never go through a second insert.
    public bool IsActive { get; set; } = true;
}
