namespace philcare.Api.Features.Programs.Activities.CreateActivity;

public sealed record CreateActivityRequest(
    int ProjectId,
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
    int? ImplementingPartnerId,
    string? ResponsibleDepartment,
    string? SdgAlignment,
    string? SafeguardingRisk,
    string? EvidenceLink,
    string? Notes);

public sealed record CreateActivityResponse(
    int Id,
    int ProjectId,
    string Name,
    string? ActivityCategory,
    string ActivityType,
    string? TargetGroup,
    decimal Budget,
    string ImplementationStatus,
    bool IsActive);
