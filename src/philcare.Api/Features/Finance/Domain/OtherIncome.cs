using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

/// <summary>
/// Non-donation inflow — the org's other funding sources besides Donation (capital/investment
/// returns, grants, waqf/endowment income, government support, partner contributions, in-kind,
/// internal transfers, reserve draws, earned income, etc — see lookup category income_type).
/// Unlike Donation there is no program/admin split: the full amount allocates into ONE chosen
/// funding bucket (admin-rate caps are donation policy — see CreateOtherIncomeHandler).
/// </summary>
public class OtherIncome : Entity
{
    public string IncomeType { get; set; } = string.Empty; // lookup: income_type
    public string Source { get; set; } = string.Empty; // payer / source description

    // Money — original currency plus the converted PHP amount used for all reporting.
    public decimal AmountOriginal { get; set; }
    public string Currency { get; set; } = "PHP";
    public decimal FxRateToPhp { get; set; } = 1m;
    public decimal AmountPhp { get; set; }

    public DateTime DateReceived { get; set; }
    public int ReportingYear { get; set; }

    // Denormalized from FundingBucket.FundCode at create time (same pattern as Expense).
    public string FundCode { get; set; } = string.Empty;
    public Fund Fund { get; set; } = null!;

    public string FundingBucketCode { get; set; } = string.Empty;
    public FundingBucket FundingBucket { get; set; } = null!;

    public string? ReceiptNo { get; set; }
    public string? EvidenceLink { get; set; }
    public string? Notes { get; set; }
    public bool IsVoided { get; set; }

    public List<Allocation> Allocations { get; set; } = [];
}
