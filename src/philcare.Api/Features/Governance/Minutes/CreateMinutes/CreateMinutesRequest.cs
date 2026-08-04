namespace philcare.Api.Features.Governance.Minutes.CreateMinutes;

public sealed record CreateMinutesRequest(
    int? PreparedByPersonId,
    int? ApprovedByPersonId,
    string? Summary,
    DateTime? NextMeetingDate,
    string? DocumentLink);

public sealed record CreateMinutesResponse(int Id, int MeetingId, string PublicationStatus);
