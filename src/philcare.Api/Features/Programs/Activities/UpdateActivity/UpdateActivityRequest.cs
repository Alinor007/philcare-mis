namespace philcare.Api.Features.Programs.Activities.UpdateActivity;

public sealed record UpdateActivityRequest(
    string Name,
    string? ActivityCategory,
    string ActivityType,
    string? TargetGroup,
    string? Barangay,
    string? City,
    string? Province,
    string? Region,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal Budget,
    string? ImplementingPartner,
    string? ResponsibleDepartment,
    string? SdgAlignment,
    string ImplementationStatus,
    string? SafeguardingRisk,
    string? EvidenceLink,
    string? Notes,
    bool IsActive);

public sealed record UpdateActivityResponse(
    int Id, int ProjectId, string Name, string ActivityType, decimal Budget, string ImplementationStatus, bool IsActive);
