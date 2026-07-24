namespace philcare.Api.Features.Programs.ActivityParticipants.AddActivityParticipant;

public sealed record AddActivityParticipantRequest(
    int ParticipantId, string? RoleInActivity, string? AttendanceStatus, bool ConsentRequired, string? EvidenceLink, string? Remarks);

public sealed record AddActivityParticipantResponse(
    int Id, int ActivityId, int ParticipantId, string ParticipantName, string? RoleInActivity, string? AttendanceStatus);
