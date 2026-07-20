using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

public class FundBucket : Entity
{
    public string Name { get; set; } = string.Empty;
    public string FundType { get; set; } = string.Empty;

    public decimal TotalReceived { get; set; }
    public decimal AdminAllocated { get; set; }
    public decimal ProgramAllocated { get; set; }
    public decimal TotalExpensed { get; set; }

    // Spendable balance — program funds only; never allowed to go negative.
    public decimal Balance => ProgramAllocated - TotalExpensed;

    public List<Allocation> Allocations { get; set; } = [];
    public List<Expense> Expenses { get; set; } = [];
}
