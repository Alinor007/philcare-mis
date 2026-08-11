namespace philcare.Api.Features.Governance.Assignments.CreateAssignment;

public sealed record CreateAssignmentRequest(
    int PersonId,
    int OrgBodyId,
    int GovernanceRoleId,
    string? PositionTitle,
    DateTime StartDate,
    bool IsPrimary,
    bool VotingRights,
    bool IsTemporary,
    string? Notes);

public sealed record CreateAssignmentResponse(
    int Id, int PersonId, int OrgBodyId, int GovernanceRoleId, bool IsPrimary, string Status);
