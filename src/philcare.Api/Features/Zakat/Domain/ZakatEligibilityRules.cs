using System.Linq.Expressions;

namespace philcare.Api.Features.Zakat.Domain;

/// <summary>
/// Zakat eligibility policy, shared by every caller that needs to gate on an approved case.
/// A static class of domain policy, mirroring the FinanceRules / ExpensePosting idiom — there is
/// no Services/ folder in this codebase and this is not one.
///
/// Kept as a pure Expression rather than a method taking AppDbContext so it composes into an EF
/// query at the call site and stays translatable. It exists so the distribution-side gate has one
/// definition instead of one per handler: CreateDistributionHandler checks the primary recipient
/// at create, AddDistributionBeneficiaryHandler checks everyone added afterwards, and a roster
/// that could be edited past a create-time-only gate would otherwise walk straight around it.
/// </summary>
public static class ZakatEligibilityRules
{
    /// <summary>
    /// Matches this beneficiary's Approved cases that have not expired as of <paramref name="asOf"/>.
    /// A null ValidUntil means the approval does not expire.
    /// </summary>
    public static Expression<Func<ZakatEligibility, bool>> ApprovedAndUnexpiredFor(int beneficiaryId, DateTime asOf) =>
        z => z.BeneficiaryId == beneficiaryId
            && z.Status == ZakatEligibilityStatus.Approved
            && (z.ValidUntil == null || z.ValidUntil >= asOf);
}
