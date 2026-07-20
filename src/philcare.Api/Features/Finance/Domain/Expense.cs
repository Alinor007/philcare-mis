using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

public class Expense : Entity
{
    public int FundBucketId { get; set; }
    public FundBucket FundBucket { get; set; } = null!;

    public decimal Amount { get; set; }
    public string ExpenseCategory { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }

    // Required when the fund bucket's fund type is zakat.
    public string? ZakatAsnaf { get; set; }
    public int? BeneficiaryCount { get; set; }

    public bool IsVoided { get; set; }
}
