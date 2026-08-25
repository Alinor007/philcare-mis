using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Domain;

/// <summary>
/// Reach roster for a <see cref="Distribution"/> — who actually received aid at one distribution
/// event. Sits alongside <see cref="ActivityParticipant"/> (the Activity staffing roster) as the
/// other half of "who was involved": that one records who delivered, this one records who received.
///
/// It carries NO quantity and NO value, deliberately. A distribution event hands the same thing to
/// everyone on this list, so the money stays entirely on the header (Quantity × UnitValuePhp) and
/// the single Expense posted at create time. Adding or removing a row here moves no money and
/// touches no funding bucket — Expense is post-once/void-only, and there is no amend path in
/// Finance to reach for. Per-person amounts would require one.
///
/// The one exception to "never amend the Expense": <c>DistributionReach.Sync</c> writes the row
/// count back to <c>Expense.BeneficiaryCount</c>. That is a reporting attribute, never an amount —
/// the zakat asnaf report reads reach off Expense rows, so leaving it frozen at 0 would silently
/// zero out zakat reporting. No balance, bucket, or total is touched.
///
/// There is no primary row. Every row is an equal recipient, and all of them are removable —
/// the distribution simply drops to a lower reach count.
/// </summary>
public class DistributionBeneficiary : Entity
{
    public int DistributionId { get; set; }
    public Distribution Distribution { get; set; } = null!;

    public int BeneficiaryId { get; set; }
    public Beneficiary Beneficiary { get; set; } = null!;

    /// <summary>Per-person receipt sign-off, distinct from the header's event-level flag.</summary>
    public bool ReceivedConfirmation { get; set; }

    public string? EvidenceLink { get; set; }
    public string? Remarks { get; set; }

    // Soft delete — removal keeps the audit trail. The unique (DistributionId, BeneficiaryId)
    // index means re-adding a removed beneficiary reactivates this same row rather than inserting
    // a second one (see AddDistributionBeneficiaryHandler).
    public bool IsActive { get; set; } = true;
}
