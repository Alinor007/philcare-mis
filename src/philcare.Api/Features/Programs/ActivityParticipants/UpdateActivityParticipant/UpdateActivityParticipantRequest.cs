namespace philcare.Api.Features.Programs.ActivityParticipants.UpdateActivityParticipant;

public sealed record UpdateActivityParticipantRequest(
    string? RoleInActivity, string? AttendanceStatus, bool ConsentRequired, string? EvidenceLink, string? Remarks);

public sealed record UpdateActivityParticipantResponse(
    int Id, int ActivityId, int StaffMemberId, string StaffMemberName, string? RoleInActivity, string? AttendanceStatus);
