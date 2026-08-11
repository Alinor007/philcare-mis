using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Governance.Domain;

/// <summary>
/// Role master (Chairperson, Vice Chair, Secretary, Treasurer, CEO, Member, ...).
/// DefaultVotingRights/CountsForQuorum/Delegable are kept as free-text rules ("Depends on
/// body", "Yes if eligible", "Per executive process") rather than booleans — the source
/// governance manual encodes conditional policy here, not a fixed answer. Actual per-instance
/// enforcement (does THIS assignment vote, does THIS meeting attendance count for quorum)
/// lives on Assignment/MeetingParticipant, which are real booleans.
/// </summary>
public class GovernanceRole : Entity
{
    public string Name { get; set; } = string.Empty;
    public string RoleCategory { get; set; } = string.Empty; // lookup: governance_role_category

    public int? DefaultBodyId { get; set; }
    public OrgBody? DefaultBody { get; set; }

    public string? DefaultVotingRights { get; set; }
    public string? CountsForQuorum { get; set; }
    public string? Delegable { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Assignment> Assignments { get; set; } = [];
}
