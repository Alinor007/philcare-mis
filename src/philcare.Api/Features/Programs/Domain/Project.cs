using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Domain;

public class Project : Entity
{
    public int ProgramId { get; set; }
    public AidProgram Program { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    // Optional links into Finance — informational only, validated at the handler level
    // rather than enforced as DB foreign keys, to keep the two modules loosely coupled.

    public int? DonorId { get; set; } //can be removed
    public string? FundCode { get; set; } //can be removed

    public decimal TotalBudget { get; set; }
    public int? TargetBeneficiaries { get; set; } //can be removed
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; } // free-text location description (e.g. "Region 1, Province A, Municipality B") Can be removed if we implement a more structured location model later.
    public string? ProjectManager { get; set; }
    public string ImplementationStatus { get; set; } = "PLANNED"; // lookup: implementation_status
    public string? ApprovalLevel { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Set when ImplementationStatus transitions to COMPLETED via ChangeProjectStatus — the
    // project-closeout timestamp. Null until then; never set any other way.
    public DateTime? ClosedAt { get; set; }

    public List<Activity> Activities { get; set; } = [];
}
