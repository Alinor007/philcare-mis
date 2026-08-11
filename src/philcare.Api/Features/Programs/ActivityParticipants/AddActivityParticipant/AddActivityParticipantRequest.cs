namespace philcare.Api.Features.Programs.ActivityParticipants.AddActivityParticipant;

public sealed record AddActivityParticipantRequest(
    int StaffMemberId, string? RoleInActivity, string? AttendanceStatus, bool ConsentRequired, string? EvidenceLink, string? Remarks);

public sealed record AddActivityParticipantResponse(
    int Id, int ActivityId, int StaffMemberId, string StaffMemberName, string? RoleInActivity, string? AttendanceStatus);
