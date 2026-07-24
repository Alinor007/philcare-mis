using philcare.Api.Common.Domain;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Sponsorships.Domain;

public enum SponsorshipStatus
{
    Active,
    Paused,
    Ended
}

/// <summary>
/// Recurring Donor↔Participant pledge (child/family/orphan/student sponsorship). MonthlyAmountPhp
/// is a pledge commitment only — actual payments are recorded as ordinary Finance Donations;
/// Expense remains the single source of money movement (Sprint 3 precedent).
/// </summary>
public class Sponsorship : Entity
{
    public int DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;

    public string SponsorshipType { get; set; } = string.Empty; // lookup: sponsorship_type
    public decimal MonthlyAmountPhp { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public SponsorshipStatus Status { get; set; } = SponsorshipStatus.Active;
    public string? CaseWorker { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public string? Notes { get; set; }
}
