using philcare.Api.Features.Finance.Domain;
using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Domain;

/// <summary>
/// A generic aid distribution (cash, food, hygiene kit, etc — see distribution_type lookup).
///
/// A distribution is a handout EVENT, not a handout to one person: it is created with no
/// recipients at all, and beneficiaries are added afterwards through the
/// <see cref="Beneficiaries"/> roster. It previously carried a single non-nullable BeneficiaryId
/// FK; that could not express one event reaching many people, so it was removed.
///
/// Recording a distribution posts a linked Finance Expense in the same save (see ExpensePosting
/// and CreateDistributionHandler) — Expense remains the single source of truth for money; this
/// entity never mutates a FundingBucket itself. TotalValuePhp is server-computed as
/// Quantity × UnitValuePhp, and reach is deliberately excluded from that formula: adding people
/// to the roster records who was reached, it never changes what was spent.
/// ExpenseId is null for every distribution recorded before this — that history is never
/// backfilled, since backfilling would retroactively consume live bucket balances — and null for
/// any zero-value in-kind handout, which posts no expense at all.
/// </summary>
public class Distribution : Entity
{
    public string DistributionType { get; set; } = string.Empty; // lookup: distribution_type

    public int? ActivityId { get; set; }
    public Activity? Activity { get; set; }

    // FundingBucketCode is required in the API contract (CreateDistributionValidator), but stays
    // nullable here — historical rows have none, and MariaDB 10.4 can't MODIFY COLUMN ... NOT NULL
    // additively. Still not a DB foreign key, to keep Programs/Finance loosely coupled; existence
    // and active-state are checked at the handler level instead.
    public string? FundingBucketCode { get; set; }

    public int Quantity { get; set; } = 1;
    public decimal UnitValuePhp { get; set; }
    public decimal TotalValuePhp { get; set; }

    /// <summary>
    /// People reached — VERIFIED, not planned: it is the count of active
    /// <see cref="Beneficiaries"/> roster rows, recomputed by <c>DistributionReach.Sync</c> on
    /// every roster add and remove. Never client-supplied. Stored rather than computed so list
    /// queries and reports don't need a subquery per row.
    ///
    /// It is 0 on a freshly created distribution, which is legitimate — the aid is booked before
    /// the officer has finished recording who received it.
    /// </summary>
    public int BeneficiaryCount { get; set; } = 0;
    public DateTime DistributionDate { get; set; }
    public string? Location { get; set; }
    public bool FieldVerified { get; set; }
    public bool ReceivedConfirmation { get; set; }
    public string? ProcessedBy { get; set; }
    public string? ZakatAsnaf { get; set; }
    public string? Notes { get; set; }
    public bool IsVoided { get; set; }

    // Null forever for historical rows and zero-value in-kind handouts (see class doc). Never a
    // DB foreign key back the other way — Expense doesn't need to know a Distribution created it.
    public int? ExpenseId { get; set; }
    public Expense? Expense { get; set; }

    /// <summary>
    /// Everyone who received aid at this event — the only record of who was reached. Reach only:
    /// carries no money. See <see cref="DistributionBeneficiary"/>.
    /// </summary>
    public List<DistributionBeneficiary> Beneficiaries { get; set; } = [];
}
