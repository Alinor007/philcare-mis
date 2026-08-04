using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Minutes.UpdateMinutes;

public sealed record UpdateMinutesRequest(
    int? PreparedByPersonId,
    int? ApprovedByPersonId,
    string? Summary,
    DateTime? NextMeetingDate,
    string? DocumentLink,
    MinutesStatus PublicationStatus);

public sealed record UpdateMinutesResponse(int Id, int MeetingId, string PublicationStatus);
