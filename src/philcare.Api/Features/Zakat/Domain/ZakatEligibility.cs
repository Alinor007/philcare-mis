using philcare.Api.Common.Domain;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Zakat.Domain;

public enum ZakatEligibilityStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected
}

/// <summary>
/// Formal zakat-aid case assessment + approval. Feeds Distribution.ZakatAsnaf: a zakat-bucket
/// distribution requires an Approved, unexpired eligibility for the participant
/// (see CreateDistributionHandler).
/// </summary>
public class ZakatEligibility : Entity
{
    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;

    public string AsnafCategory { get; set; } = string.Empty; // lookup: zakat_asnaf
    public decimal? MonthlyIncomePhp { get; set; }
    public int? HouseholdSize { get; set; }
    public DateTime AssessmentDate { get; set; }
    public string? AssessedBy { get; set; }
    public string? AssessmentNotes { get; set; }

    public ZakatEligibilityStatus Status { get; set; } = ZakatEligibilityStatus.Draft;
    public DateTime? DecisionDate { get; set; }
    public string? DecidedBy { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
}
