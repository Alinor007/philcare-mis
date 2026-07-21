using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

public enum AllocationType
{
    Program,
    Admin,
    Amil,
    Opening
}

/// <summary>
/// A single ledger line produced by splitting a donation's gross amount into a target
/// funding bucket. A donation typically produces one Program line and, when admin/amil
/// is allowed, one Admin or Amil line.
/// </summary>
public class Allocation : Entity
{
    public DateTime AllocationDate { get; set; }

    public int? DonationId { get; set; }
    public Donation? Donation { get; set; }

    public string SourceFundCode { get; set; } = string.Empty;
    public Fund SourceFund { get; set; } = null!;

    public decimal GrossAmountPhp { get; set; }
    public AllocationType AllocationType { get; set; }
    public decimal AllocationRate { get; set; }
    public decimal AllocatedAmountPhp { get; set; }

    public string TargetBucketCode { get; set; } = string.Empty;
    public FundingBucket TargetBucket { get; set; } = null!;

    public int ReportingYear { get; set; }
    public decimal PolicyCap { get; set; }
    public string Status { get; set; } = "OK";
    public string? EvidenceNotes { get; set; }
}
