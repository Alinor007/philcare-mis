namespace philcare.Api.Features.Governance.Decisions.CreateDecision;

public sealed record CreateDecisionRequest(
    string DecisionText,
    string? ActionPoints,
    int? ResponsiblePersonId,
    DateTime? DueDate,
    string DecisionStatus,
    string? Notes);

public sealed record CreateDecisionResponse(int Id, int MeetingMinutesId, string DecisionText, string DecisionStatus);
