using philcare.Api.Common.Domain;
using philcare.Api.Features.People.Domain;

namespace philcare.Api.Features.Finance.Domain;

public class Expense : Entity
{
    public DateTime ExpenseDate { get; set; }
    public string PayeeVendor { get; set; } = string.Empty;
    public string ExpenseCategory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string FundCode { get; set; } = string.Empty;
    public Fund Fund { get; set; } = null!;

    public string? ProgramOrProject { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;

    public decimal AmountOriginal { get; set; }
    public string Currency { get; set; } = "PHP";
    public decimal FxRateToPhp { get; set; } = 1m;
    public decimal AmountPhp { get; set; }

    public string? ReceiptNo { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";

    // A real Person reference, not free text — matches MeetingMinutes.ApprovedByPersonId. Optional:
    // an expense can be posted (e.g. via a linked Distribution) without a named approver.
    public int? ApprovedByPersonId { get; set; }
    public Person? ApprovedByPerson { get; set; }
    public string? SupportingDocStatus { get; set; }
    public int? LinkedDonationId { get; set; }

    public string? ExpenseFunction { get; set; }

    public string FundingBucketCode { get; set; } = string.Empty;
    public FundingBucket FundingBucket { get; set; } = null!;

    // Required when the funding bucket sits under the Zakat fund's Program bucket.
    public string? ZakatAsnaf { get; set; }
    public int? BeneficiaryCount { get; set; }
    public string? BeneficiaryType { get; set; }

    public string? AdminEligibility { get; set; }
    public string? Notes { get; set; }
    public bool IsVoided { get; set; }
}
