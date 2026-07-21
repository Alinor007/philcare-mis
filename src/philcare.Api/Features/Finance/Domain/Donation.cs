using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

public class Donation : Entity
{
    public int DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    // Money — original currency plus the converted PHP amount used for all reporting.
    public decimal AmountOriginal { get; set; }
    public string Currency { get; set; } = "PHP";
    public decimal FxRateToPhp { get; set; } = 1m;
    public decimal AmountPhp { get; set; }

    public DateTime DateReceived { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public bool RestrictedFlag { get; set; }
    public string? ProgramOrProject { get; set; }

    public string FundCode { get; set; } = string.Empty;
    public Fund Fund { get; set; } = null!;

    public string? ReceiptNo { get; set; }

    // Compliance / AML
    public string? CashDocumentationStatus { get; set; }
    public bool SourceVerified { get; set; }
    public bool AmlReviewFlag { get; set; }
    public KydStatus KydStatus { get; set; } = KydStatus.Pending;
    public RiskRating RiskRating { get; set; } = RiskRating.Low;
    public string? ComplianceCheck { get; set; }

    // Allocation inputs and computed results (see FinanceRules for caps)
    public bool AdminAllowed { get; set; }
    public decimal AdminRateInput { get; set; }
    public decimal AdminRateCap { get; set; }
    public decimal AdminRateApplied { get; set; }
    public decimal ProgramAllocationPhp { get; set; }
    public decimal AdminAllocationPhp { get; set; }
    public string? ProgramBucketCode { get; set; }
    public string? AdminBucketCode { get; set; }
    public string AllocationStatus { get; set; } = "Pending";

    public string? Notes { get; set; }
    public bool IsVoided { get; set; }

    public List<Allocation> Allocations { get; set; } = [];
}
