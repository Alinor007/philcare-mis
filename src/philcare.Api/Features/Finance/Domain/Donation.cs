using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

public class Donation : Entity
{
    public int DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PHP";
    public string FundType { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;

    public bool AdminAllowed { get; set; }
    public decimal AdminRate { get; set; }
    public decimal AmilRate { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public bool IsVoided { get; set; }

    public Allocation? Allocation { get; set; }
}
