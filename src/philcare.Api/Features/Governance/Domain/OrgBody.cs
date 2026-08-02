using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Governance.Domain;

/// <summary>
/// Governance body in a self-referencing hierarchy (General Assembly → Board of Trustees →
/// Executive Management → operational units). QuorumRule/DecisionThreshold/MeetingFrequency
/// are kept as free-text policy strings verbatim from the org's governance manual (e.g.
/// "50% + 1", "75% for strategic decisions") rather than parsed into numbers — the quorum
/// endpoint reports counts alongside the rule text but does not attempt to evaluate it.
/// </summary>
public class OrgBody : Entity
{
    public string Name { get; set; } = string.Empty;
    public string BodyType { get; set; } = string.Empty; // lookup: org_body_type

    public int? ParentBodyId { get; set; }
    public OrgBody? ParentBody { get; set; }

    public string? QuorumRule { get; set; }
    public string? DecisionThreshold { get; set; }
    public string? MeetingFrequency { get; set; }
    public string? PolicyBasis { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<OrgBody> ChildBodies { get; set; } = [];
    public List<Assignment> Assignments { get; set; } = [];
    public List<Meeting> Meetings { get; set; } = [];
}
