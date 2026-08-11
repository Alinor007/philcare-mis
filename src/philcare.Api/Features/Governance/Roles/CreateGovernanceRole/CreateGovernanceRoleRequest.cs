namespace philcare.Api.Features.Governance.Roles.CreateGovernanceRole;

public sealed record CreateGovernanceRoleRequest(
    string Name,
    string RoleCategory,
    int? DefaultBodyId,
    string? DefaultVotingRights,
    string? CountsForQuorum,
    string? Delegable,
    string? Notes);

public sealed record CreateGovernanceRoleResponse(int Id, string Name, string RoleCategory, bool IsActive);
