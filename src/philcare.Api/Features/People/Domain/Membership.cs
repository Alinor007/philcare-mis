using philcare.Api.Common.Domain;

namespace philcare.Api.Features.People.Domain;

/// <summary>
/// Registered-membership profile for a <see cref="Person"/> — the org's formal member roll, with
/// its own numbering and renewal cycle.
///
/// Distinct from Governance Assignment: an Assignment is a role held in an OrgBody (trustee,
/// treasurer, committee member) and is what board/executive rosters derive from. A Membership is
/// standing in the organisation itself, independent of whether the member holds any role. The
/// General Assembly is modelled as an OrgBody, so a member may well have both.
///
/// Unlike StaffMember and Volunteer this is NOT one-per-person: a lapsed membership that is later
/// renewed under a new number is a second row, so the roll keeps its own history. ExitDate plus
/// Status is what marks a row closed.
/// </summary>
public class Membership : Entity
{
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    /// <summary>Org-assigned identifier, unique across the whole roll.</summary>
    public string MembershipNumber { get; set; } = string.Empty;

    public string MembershipType { get; set; } = string.Empty; // lookup: membership_type
    public string Status { get; set; } = "ACTIVE";             // lookup: membership_status

    public DateTime? JoinDate { get; set; }
    public DateTime? RenewalDate { get; set; }
    public DateTime? ExitDate { get; set; }

    // Free text, deliberately not a Person FK: referrals are often recorded as a name the org has
    // no record for, and forcing a link would block the entry.
    public string? ReferredBy { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
