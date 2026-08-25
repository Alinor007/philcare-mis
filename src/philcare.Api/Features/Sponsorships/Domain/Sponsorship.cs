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
/// Recurring Donor↔Beneficiary pledge (child/family/orphan/student sponsorship). MonthlyAmountPhp
/// is a pledge commitment only — actual payments are recorded as ordinary Finance Donations;
/// Expense remains the single source of money movement (Sprint 3 precedent).
/// </summary>
public class Sponsorship : Entity
{
    public int DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public int BeneficiaryId { get; set; }
    public Beneficiary Beneficiary { get; set; } = null!;

    public string SponsorshipType { get; set; } = string.Empty; // lookup: sponsorship_type
    public decimal MonthlyAmountPhp { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public SponsorshipStatus Status { get; set; } = SponsorshipStatus.Active;
    public string? CaseWorker { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// true only while this pledge is live (Active or Paused); null once Ended. Backed by a unique
    /// index on (DonorId, BeneficiaryId, IsActiveSponsorship) — NULLs never collide, so at most one
    /// live pledge can exist per donor/beneficiary pair. This is the MariaDB 10.4 substitute for a
    /// filtered unique index and is what actually closes the concurrent-create race; the handler's
    /// AnyAsync pre-check is only a friendly message for the common case. Mirrors
    /// ZakatEligibility.IsLiveApproval deliberately — same problem, same shape.
    /// Reads still filter on Status; this flag exists purely for the DB-level guarantee.
    /// </summary>
    public bool? IsActiveSponsorship { get; set; }
}
