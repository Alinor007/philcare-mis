using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Domain;

public class Project : Entity
{
    public int ProgramId { get; set; }
    public AidProgram Program { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    // Optional links into Finance — informational only, validated at the handler level
    // rather than enforced as DB foreign keys, to keep the two modules loosely coupled.
    public int? DonorId { get; set; }
    public string? FundCode { get; set; }

    public decimal TotalBudget { get; set; }
    public int? TargetBeneficiaries { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; }
    public string? ProjectManager { get; set; }
    public string ImplementationStatus { get; set; } = "PLANNED"; // lookup: implementation_status
    public string? ApprovalLevel { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Activity> Activities { get; set; } = [];
}
