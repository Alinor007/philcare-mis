using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Domain;

/// <summary>
/// A generic aid distribution (cash, food, hygiene kit, etc — see distribution_type lookup).
/// TotalValuePhp is informational for program reporting; it does NOT move Finance funding-bucket
/// balances. Money movement stays exclusively with Finance Expense (single source of truth).
/// </summary>
public class Distribution : Entity
{
    public string DistributionType { get; set; } = string.Empty; // lookup: distribution_type

    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;

    public int? ActivityId { get; set; }
    public Activity? Activity { get; set; }

    // Optional link into Finance — validated at the handler level (bucket must exist; if it's
    // the zakat program bucket, ZakatAsnaf is required), not enforced as a DB foreign key.
    public string? FundingBucketCode { get; set; }

    public int Quantity { get; set; } = 1;
    public decimal TotalValuePhp { get; set; }
    public DateTime DistributionDate { get; set; }
    public string? Location { get; set; }
    public bool FieldVerified { get; set; }
    public bool ReceivedConfirmation { get; set; }
    public string? ProcessedBy { get; set; }
    public string? ZakatAsnaf { get; set; }
    public string? Notes { get; set; }
    public bool IsVoided { get; set; }
}
