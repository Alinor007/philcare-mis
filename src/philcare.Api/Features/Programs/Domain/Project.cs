using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Domain;

public class Project : Entity
{
    public int ProgramId { get; set; }
    public AidProgram Program { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    // Which category of money funds this project. Informational only — it is a lookup label,
    // not a link into Finance, so it stays loosely coupled the same way FundCode/DonorId were.
    // Actual money movement is unaffected: Donation/Expense funding is wired through
    // Fund/FundingBucket, never through this field.
    public string? FundType { get; set; } // lookup: project_fund_type

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

    public List<Distribution> Distributions { get; set } = [];

    /// <summary>Donors supporting this project — a project can have several. See ProjectDonor.</summary>
    public List<ProjectDonor> Donors { get; set; } = [];
}
