namespace philcare.Api.Features.Governance.Meetings.CreateMeeting;

public sealed record CreateMeetingRequest(
    int OrgBodyId,
    string MeetingType,
    DateTime MeetingDate,
    string Mode,
    string? CalledBy,
    int? ChairPersonId,
    int? SecretaryPersonId,
    DateTime? PublicationDeadline,
    string? Notes);

public sealed record CreateMeetingResponse(
    int Id, int OrgBodyId, string MeetingType, DateTime MeetingDate, string Status, string? QuorumRequired, string? DecisionThreshold);
