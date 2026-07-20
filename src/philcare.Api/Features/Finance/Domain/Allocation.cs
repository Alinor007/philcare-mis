using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

public class Allocation : Entity
{
    public int DonationId { get; set; }
    public Donation Donation { get; set; } = null!;

    public int FundBucketId { get; set; }
    public FundBucket FundBucket { get; set; } = null!;

    public decimal ProgramAmount { get; set; }
    public decimal AdminAmount { get; set; }
    public decimal AmilAmount { get; set; }
}
