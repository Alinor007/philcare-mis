namespace philcare.Api.Features.Governance.OrgBodies.UpdateOrgBody;

public sealed record UpdateOrgBodyRequest(
    string Name,
    string BodyType,
    int? ParentBodyId,
    string? QuorumRule,
    string? DecisionThreshold,
    string? MeetingFrequency,
    string? PolicyBasis,
    string? Notes,
    bool IsActive);

public sealed record UpdateOrgBodyResponse(int Id, string Name, string BodyType, int? ParentBodyId, bool IsActive);
