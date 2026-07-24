namespace philcare.Api.Features.Programs.Projects.CreateProject;

public sealed record CreateProjectRequest(
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
    string? ApprovalLevel,
    string? Notes);

public sealed record CreateProjectResponse(
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
