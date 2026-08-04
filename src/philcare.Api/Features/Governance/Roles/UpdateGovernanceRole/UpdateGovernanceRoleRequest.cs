namespace philcare.Api.Features.Governance.Roles.UpdateGovernanceRole;

public sealed record UpdateGovernanceRoleRequest(
    string Name,
    string RoleCategory,
    int? DefaultBodyId,
    string? DefaultVotingRights,
    string? CountsForQuorum,
    string? Delegable,
    string? Notes,
    bool IsActive);

public sealed record UpdateGovernanceRoleResponse(int Id, string Name, string RoleCategory, bool IsActive);
