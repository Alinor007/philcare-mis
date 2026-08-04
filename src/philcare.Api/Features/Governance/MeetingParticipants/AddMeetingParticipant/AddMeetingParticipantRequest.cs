namespace philcare.Api.Features.Governance.MeetingParticipants.AddMeetingParticipant;

public sealed record AddMeetingParticipantRequest(
    int PersonId,
    int? AssignmentId,
    string? RoleInMeeting,
    string AttendanceStatus,
    bool VotingRight,
    bool CountsForQuorum,
    string? ParticipationMode,
    string? Remarks);

public sealed record AddMeetingParticipantResponse(int Id, int MeetingId, int PersonId, string PersonFullName, string AttendanceStatus);
