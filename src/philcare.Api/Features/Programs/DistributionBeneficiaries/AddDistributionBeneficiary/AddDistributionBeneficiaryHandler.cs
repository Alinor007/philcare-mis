using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.Programs.Domain;
using philcare.Api.Features.Zakat.Domain;

namespace philcare.Api.Features.Programs.DistributionBeneficiaries.AddDistributionBeneficiary;

/// <summary>
/// Adds a recipient to a distribution's reach roster. Posts no money and touches no funding
/// bucket — the event's cost was fixed and expensed when the distribution was created.
/// </summary>
public sealed class AddDistributionBeneficiaryHandler(AppDbContext db)
{
    public async Task<Result<AddDistributionBeneficiaryResponse>> HandleAsync(
        int distributionId, AddDistributionBeneficiaryRequest request, CancellationToken cancellationToken)
    {
        // Roster and expense are loaded because DistributionReach.Sync writes to both below.
        var distribution = await db.Distributions
            .Include(d => d.Beneficiaries)
            .Include(d => d.Expense)
            .FirstOrDefaultAsync(d => d.Id == distributionId, cancellationToken);

        if (distribution is null)
        {
            return Result.Failure<AddDistributionBeneficiaryResponse>(
                Error.NotFound("DistributionBeneficiaries.DistributionNotFound", "Distribution not found."));
        }

        // A voided distribution has had its expense reversed; adding reach to it would claim the
        // org delivered aid it has already un-booked.
        if (distribution.IsVoided)
        {
            return Result.Failure<AddDistributionBeneficiaryResponse>(
                Error.Conflict("DistributionBeneficiaries.DistributionVoided", "Cannot add beneficiaries to a voided distribution."));
        }

        var beneficiary = await db.Beneficiaries.FirstOrDefaultAsync(b => b.Id == request.BeneficiaryId, cancellationToken);

        if (beneficiary is null)
        {
            return Result.Failure<AddDistributionBeneficiaryResponse>(
                Error.NotFound("DistributionBeneficiaries.BeneficiaryNotFound", "Beneficiary not found."));
        }

        if (!beneficiary.IsActive)
        {
            return Result.Failure<AddDistributionBeneficiaryResponse>(
                Error.Validation("DistributionBeneficiaries.BeneficiaryInactive", "Cannot record aid for an inactive beneficiary."));
        }

        // The real consent gate, which the Activity roster lost when it pivoted to staff. Registration
        // already refuses without consent, so this only catches a record whose consent was withdrawn
        // after the fact.
        if (!beneficiary.ConsentOnFile)
        {
            return Result.Failure<AddDistributionBeneficiaryResponse>(
                Error.Validation("DistributionBeneficiaries.ConsentRequired", "Consent must be on file before aid can be recorded for this beneficiary."));
        }

        var zakatGate = await CheckZakatEligibilityAsync(distribution, beneficiary.Id, cancellationToken);

        if (zakatGate.IsFailure)
        {
            return Result.Failure<AddDistributionBeneficiaryResponse>(zakatGate.Error);
        }

        // Soft delete means a prior removal leaves the (DistributionId, BeneficiaryId) row behind —
        // the unique index means a re-add must reactivate it, never insert a second row.
        var existingRow = await db.DistributionBeneficiaries
            .FirstOrDefaultAsync(r => r.DistributionId == distributionId && r.BeneficiaryId == request.BeneficiaryId, cancellationToken);

        if (existingRow is { IsActive: true })
        {
            return Result.Failure<AddDistributionBeneficiaryResponse>(
                Error.Conflict("DistributionBeneficiaries.AlreadyAdded", "This beneficiary is already on this distribution's roster."));
        }

        // Double-issue guard. The unique index above already stops the same person being added
        // twice to THIS distribution; this catches the same person being issued the same aid type
        // twice under the same activity on the same day via a second distribution record — the
        // double entry a re-keyed paper form or a double-clicked create produces. Legitimate
        // (two stations, a top-up), so the officer can confirm rather than being blocked.
        if (!request.ConfirmDuplicate)
        {
            var duplicate = await FindSameDayIssueAsync(distribution, request.BeneficiaryId, cancellationToken);

            if (duplicate is not null)
            {
                return Result.Failure<AddDistributionBeneficiaryResponse>(
                    Error.Conflict("DistributionBeneficiaries.PossibleDuplicate",
                        $"{beneficiary.FullName} was already recorded as receiving {distribution.DistributionType} "
                        + $"under this activity on {distribution.DistributionDate.Date:d MMM yyyy} "
                        + $"(distribution #{duplicate.Value}). Confirm to record this as a separate handout."));
            }
        }

        DistributionBeneficiary row;

        if (existingRow is not null)
        {
            existingRow.ReceivedConfirmation = request.ReceivedConfirmation;
            existingRow.EvidenceLink = request.EvidenceLink;
            existingRow.Remarks = request.Remarks;
            existingRow.IsActive = true;
            row = existingRow;
        }
        else
        {
            row = new DistributionBeneficiary
            {
                DistributionId = distributionId,
                BeneficiaryId = request.BeneficiaryId,
                ReceivedConfirmation = request.ReceivedConfirmation,
                EvidenceLink = request.EvidenceLink,
                Remarks = request.Remarks
            };
            db.DistributionBeneficiaries.Add(row);
            // Sync counts off the loaded navigation, so a newly inserted row has to join it.
            distribution.Beneficiaries.Add(row);
        }

        DistributionReach.Sync(distribution);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new AddDistributionBeneficiaryResponse(
            row.Id, row.DistributionId, row.BeneficiaryId, beneficiary.FullName,
            row.ReceivedConfirmation, distribution.BeneficiaryCount));
    }

    /// <summary>
    /// Returns the id of another live distribution that already issued this aid type to this
    /// beneficiary, under the same activity, on the same calendar day — or null if there is none.
    /// </summary>
    private async Task<int?> FindSameDayIssueAsync(Distribution distribution, int beneficiaryId, CancellationToken cancellationToken)
    {
        if (distribution.ActivityId is null)
        {
            return null;
        }

        var day = distribution.DistributionDate.Date;

        var match = await db.DistributionBeneficiaries
            .Where(r => r.IsActive
                && r.BeneficiaryId == beneficiaryId
                && r.DistributionId != distribution.Id
                && !r.Distribution.IsVoided
                && r.Distribution.ActivityId == distribution.ActivityId
                && r.Distribution.DistributionType == distribution.DistributionType
                && r.Distribution.DistributionDate >= day
                && r.Distribution.DistributionDate < day.AddDays(1))
            .Select(r => (int?)r.DistributionId)
            .FirstOrDefaultAsync(cancellationToken);

        return match;
    }

    /// <summary>
    /// Mirrors the create-time gate in CreateDistributionHandler: aid drawn from the zakat program
    /// bucket requires an approved, unexpired eligibility whose asnaf matches the one the event was
    /// booked under. Without this, the roster would be a way to reach the zakat bucket for people
    /// who were never assessed.
    /// </summary>
    private async Task<Result> CheckZakatEligibilityAsync(Distribution distribution, int beneficiaryId, CancellationToken cancellationToken)
    {
        // Historical rows predate the required bucket code; nothing to resolve, nothing to gate.
        if (string.IsNullOrWhiteSpace(distribution.FundingBucketCode))
        {
            return Result.Success();
        }

        var bucket = await db.FundingBuckets.FirstOrDefaultAsync(b => b.Code == distribution.FundingBucketCode, cancellationToken);

        if (bucket is null || !ExpensePosting.IsZakatProgramBucket(bucket))
        {
            return Result.Success();
        }

        var approvedEligibility = await db.ZakatEligibilities
            .Where(ZakatEligibilityRules.ApprovedAndUnexpiredFor(beneficiaryId, distribution.DistributionDate))
            .OrderByDescending(z => z.DecisionDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (approvedEligibility is null)
        {
            return Result.Failure(
                Error.Validation("DistributionBeneficiaries.ZakatEligibilityRequired",
                    "This beneficiary needs an approved, unexpired zakat eligibility case before they can be added to a distribution drawn from the zakat program bucket."));
        }

        // The distribution's asnaf is already frozen into the posted Expense, which is never
        // amended — so every recipient has to belong to that same asnaf category.
        if (!string.Equals(approvedEligibility.AsnafCategory, distribution.ZakatAsnaf, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(
                Error.Validation("DistributionBeneficiaries.ZakatAsnafMismatch",
                    $"This beneficiary's approved asnaf does not match the distribution's asnaf ({distribution.ZakatAsnaf})."));
        }

        return Result.Success();
    }
}
