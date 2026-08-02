namespace philcare.Api.Features.Governance.Decisions.UpdateDecision;

public sealed record UpdateDecisionRequest(
    string DecisionText,
    string? ActionPoints,
    int? ResponsiblePersonId,
    DateTime? DueDate,
    string DecisionStatus,
    string? Notes);

public sealed record UpdateDecisionResponse(int Id, int MeetingMinutesId, string DecisionText, string DecisionStatus);
