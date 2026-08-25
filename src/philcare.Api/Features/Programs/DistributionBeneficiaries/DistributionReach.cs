using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.DistributionBeneficiaries;

/// <summary>
/// Keeps "people reached" in step with the roster. Mirrors the FinanceRules / ExpensePosting idiom
/// — a static policy class, not a service; this codebase has no Services/ folder and does not want
/// one.
///
/// Reach is stored on the Distribution rather than counted on demand so that list queries and
/// reports don't need a correlated subquery per row. The cost of that choice is exactly this
/// method: every path that adds, removes, or reactivates a roster row must call it.
/// </summary>
public static class DistributionReach
{
    /// <summary>
    /// Recomputes <c>BeneficiaryCount</c> from the distribution's loaded roster and mirrors it onto
    /// the linked Expense. Call with the roster already loaded (Include) and BEFORE SaveChangesAsync;
    /// both entities must be tracked.
    ///
    /// Writing to the Expense is the one deliberate exception to "Expense is post-once and never
    /// amended": the zakat asnaf report reads reach off Expense rows, so leaving that frozen at the
    /// posting-time 0 would silently zero out zakat reporting. Only this reporting attribute is
    /// touched — never an amount, a bucket, or a balance — so no ledger total can move as a result.
    /// A voided expense is left alone; its reach is part of the record of what was un-booked.
    /// </summary>
    public static void Sync(Distribution distribution)
    {
        // Distinct by person, not a row count. The unique (DistributionId, BeneficiaryId) index
        // already guarantees one row each, but EF's relationship fixup can also place a
        // newly-Added row into this loaded collection that the caller just appended to — counting
        // rows would then double it. Distinct is both the safe count and the honest definition of
        // reach.
        var reached = distribution.Beneficiaries
            .Where(b => b.IsActive)
            .Select(b => b.BeneficiaryId)
            .Distinct()
            .Count();

        distribution.BeneficiaryCount = reached;

        if (distribution.Expense is not null && !distribution.Expense.IsVoided)
        {
            distribution.Expense.BeneficiaryCount = reached;
        }
    }
}
