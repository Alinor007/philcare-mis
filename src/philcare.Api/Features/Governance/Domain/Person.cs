using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Governance.Domain;

/// <summary>
/// Identity hub for governance: a natural person who holds (or has held) one or more
/// Assignments to an OrgBody in a GovernanceRole — trustee, executive, or general member.
/// "Board trustees" and "executive team" are not separate entities; they are Person rows
/// with a Current Assignment to the relevant OrgBody, resolved via GET /api/governance/bodies/{id}/members.
/// </summary>
public class Person : Entity
{
    public string FullName { get; set; } = string.Empty;
    public string PersonCategory { get; set; } = string.Empty; // lookup: person_category
    public string Status { get; set; } = "ACTIVE"; // lookup: person_status
    public string? Email { get; set; }
    public string? ContactNumber { get; set; }
    public bool DefaultVotingRights { get; set; }

    // Optional loose link into Volunteers — informational only, validated at the handler
    // level rather than enforced as a DB foreign key, mirroring Project.DonorId.
    public int? VolunteerId { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Assignment> Assignments { get; set; } = [];
}
