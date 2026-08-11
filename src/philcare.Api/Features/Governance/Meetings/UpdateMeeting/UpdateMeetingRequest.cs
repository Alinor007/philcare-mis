using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Meetings.UpdateMeeting;

public sealed record UpdateMeetingRequest(
    string MeetingType,
    DateTime MeetingDate,
    string Mode,
    string? CalledBy,
    int? ChairPersonId,
    int? SecretaryPersonId,
    MeetingStatus Status,
    DateTime? PublicationDeadline,
    string? Notes);

public sealed record UpdateMeetingResponse(int Id, int OrgBodyId, string MeetingType, DateTime MeetingDate, string Status);
