namespace philcare.Api.Features.Governance.OrgBodies.CreateOrgBody;

public sealed record CreateOrgBodyRequest(
    string Name,
    string BodyType,
    int? ParentBodyId,
    string? QuorumRule,
    string? DecisionThreshold,
    string? MeetingFrequency,
    string? PolicyBasis,
    string? Notes);

public sealed record CreateOrgBodyResponse(int Id, string Name, string BodyType, int? ParentBodyId, bool IsActive);
