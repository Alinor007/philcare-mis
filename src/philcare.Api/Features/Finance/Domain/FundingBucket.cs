using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

public enum BucketType
{
    Program,
    Admin,
    Operations,
    Capital
}

/// <summary>
/// A spendable sub-division of a <see cref="Fund"/> (e.g. ZAK-PROG, ZAK-AMIL).
/// Balances are computed live: Remaining = AllocatedAmount - ExpensedAmount, never negative.
/// </summary>
public class FundingBucket : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string FundCode { get; set; } = string.Empty;
    public Fund Fund { get; set; } = null!;

    public BucketType BucketType { get; set; }

    // 0 for Program buckets; 0.125 for the Zakat Amil bucket; 0.15 for other Admin buckets; 1.0 for Operations pools.
    public decimal MaxAdminRate { get; set; }

    public string? PolicyRule { get; set; }
    public string? TypicalUse { get; set; }
    public bool SeparateTrackingRequired { get; set; }

    public decimal AllocatedAmount { get; set; }
    public decimal ExpensedAmount { get; set; }

    public decimal Remaining => AllocatedAmount - ExpensedAmount;

    public List<Allocation> Allocations { get; set; } = [];
    public List<Expense> Expenses { get; set; } = [];
}
