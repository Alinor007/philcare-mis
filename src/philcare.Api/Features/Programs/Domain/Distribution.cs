using philcare.Api.Features.Finance.Domain;
using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Domain;

/// <summary>
/// A generic aid distribution (cash, food, hygiene kit, etc — see distribution_type lookup).
/// Recording a distribution posts a linked Finance Expense in the same save (see ExpensePosting
/// and CreateDistributionHandler) — Expense remains the single source of truth for money; this
/// entity never mutates a FundingBucket itself. TotalValuePhp is server-computed as
/// Quantity × UnitValuePhp; BeneficiaryCount is reporting-only (zakat asnaf breakdowns) and is
/// deliberately excluded from the money formula, since ParticipantId is a single non-nullable FK.
/// ExpenseId is null for every distribution recorded before this — that history is never
/// backfilled, since backfilling would retroactively consume live bucket balances — and null for
/// any zero-value in-kind handout, which posts no expense at all.
/// </summary>
public class Distribution : Entity
{
    public string DistributionType { get; set; } = string.Empty; // lookup: distribution_type

    public int BeneficiaryId { get; set; }
    public Beneficiary Beneficiary { get; set; } = null!;

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
    public int BeneficiaryCount { get; set; } = 1;
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
}
