namespace philcare.Api.Features.Governance.Assignments.UpdateAssignment;

public sealed record UpdateAssignmentRequest(
    string? PositionTitle,
    bool IsPrimary,
    bool VotingRights,
    bool IsTemporary,
    string? Notes);

public sealed record UpdateAssignmentResponse(int Id, int PersonId, int OrgBodyId, int GovernanceRoleId, bool IsPrimary, string Status);
