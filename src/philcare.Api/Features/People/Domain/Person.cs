using philcare.Api.Common.Domain;
using philcare.Api.Features.Governance.Domain;
using philcare.Api.Features.HumanResources.Domain;

namespace philcare.Api.Features.People.Domain;

/// <summary>
/// Shared identity hub — one row per human being, regardless of how many roles they hold. Staff
/// (<see cref="StaffMember"/>) and Volunteer profiles hang off this via a required PersonId FK;
/// Governance Assignments, Memberships, and (in time) other role-specific records do the same.
///
/// Promoted from Governance.Person, which already carried the identity fields and a working CRUD
/// slice — widening and re-parenting an existing entity beats standing up a second person table
/// beside it. Its old doc comment still applies to the governance angle: "board trustees" and
/// "executive team" are not separate entities; they are Person rows with a Current Assignment to
/// the relevant OrgBody, resolved via GET /api/governance/bodies/{id}/members.
/// </summary>
public class Person : Entity
{
    public string FullName { get; set; } = string.Empty;
    public string PersonCategory { get; set; } = string.Empty; // lookup: person_category
    public string Status { get; set; } = "ACTIVE"; // lookup: person_status

    public string? Email { get; set; }
    public string? ContactNumber { get; set; }

    // String, not DateTime — matches Beneficiary.DateOfBirth: partial/imported dates don't always
    // parse to a real DateTime, and MariaDB datetime(6) rejects the 0001-01-01 a bad parse would
    // otherwise produce.
    public string? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Unspecified;
    public string? CivilStatus { get; set; } // lookup: civil_status

    public string? Barangay { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Region { get; set; }

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactNumber { get; set; }

    // One photo per person, not per role — StaffMember/Volunteer no longer carry their own.
    public string? PhotoUrl { get; set; }

    public bool DefaultVotingRights { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Assignment> Assignments { get; set; } = [];

    // Role-specific profiles. Null/absent means the person doesn't hold that role; presence is
    // what CreateStaffMember/CreateVolunteer check for "already has a profile", rather than a
    // separate flag on Person itself.
    public StaffMember? StaffProfile { get; set; }
    public Volunteer? VolunteerProfile { get; set; }

    // A list, not a single profile: renewals under a new number are separate rows (see Membership).
    public List<Membership> Memberships { get; set; } = [];
}
