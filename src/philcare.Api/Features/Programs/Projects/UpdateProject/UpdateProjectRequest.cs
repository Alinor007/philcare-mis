namespace philcare.Api.Features.Programs.Projects.UpdateProject;

public sealed record UpdateProjectRequest(
    string Name,
    int? DonorId,
    string? FundCode,
    decimal TotalBudget,
    int? TargetBeneficiaries,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Location,
    string? ProjectManager,
    string? ApprovalLevel,
    string? Notes,
    bool IsActive);

public sealed record UpdateProjectResponse(
    int Id,
    int ProgramId,
    string Name,
    int? DonorId,
    string? FundCode,
    decimal TotalBudget,
    int? TargetBeneficiaries,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Location,
    string? ProjectManager,
    string ImplementationStatus,
    string? ApprovalLevel,
    string? Notes,
    bool IsActive);
