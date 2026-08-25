using philcare.Api.Common.Domain;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Programs.Domain;

/// <summary>
/// Which donors support a project — a project can have several. Plain link, no per-row data:
/// unlike DistributionBeneficiary or ActivityVolunteer this carries no receipt, evidence, or
/// amount, so it hard-deletes on removal rather than soft-deleting (same shape as
/// MeetingParticipant). It moves no money and is not a Finance FK — Donation/Expense funding
/// stays wired through Fund/FundingBucket exactly as before; this only records who is
/// contributing to which project.
/// </summary>
public class ProjectDonor : Entity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int DonorId { get; set; }
    public Donor Donor { get; set; } = null!;
}
