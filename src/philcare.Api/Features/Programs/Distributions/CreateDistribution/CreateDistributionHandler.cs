using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.Programs.Domain;
using philcare.Api.Features.Zakat.Domain;

namespace philcare.Api.Features.Programs.Distributions.CreateDistribution;

public sealed class CreateDistributionHandler(AppDbContext db)
{
    public async Task<Result<CreateDistributionResponse>> HandleAsync(CreateDistributionRequest request, CancellationToken cancellationToken)
    {
        var participant = await db.Beneficiaries.FirstOrDefaultAsync(p => p.Id == request.BeneficiaryId, cancellationToken);

        if (participant is null)
        {
            return Result.Failure<CreateDistributionResponse>(Error.NotFound("Distributions.ParticipantNotFound", "Participant not found."));
        }

        if (!participant.IsActive)
        {
            return Result.Failure<CreateDistributionResponse>(
                Error.Validation("Distributions.ParticipantInactive", "Cannot record a distribution for an inactive participant."));
        }

        // Included so ProgramOrProject on the generated Expense can be resolved without a second query.
        var activity = await db.Activities.Include(a => a.Project).FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);

        if (activity is null)
        {
            return Result.Failure<CreateDistributionResponse>(Error.NotFound("Distributions.ActivityNotFound", "Activity not found."));
        }

        var bucket = await db.FundingBuckets.FirstOrDefaultAsync(b => b.Code == request.FundingBucketCode, cancellationToken);

        if (bucket is null)
        {
            return Result.Failure<CreateDistributionResponse>(Error.NotFound("Distributions.FundingBucketNotFound", "Funding bucket not found."));
        }

        var zakatAsnaf = request.ZakatAsnaf;

        if (ExpensePosting.IsZakatProgramBucket(bucket))
        {
            var approvedEligibility = await db.ZakatEligibilities
                .Where(z => z.ParticipantId == participant.Id
                    && z.Status == ZakatEligibilityStatus.Approved
                    && (z.ValidUntil == null || z.ValidUntil >= request.DistributionDate))
                .OrderByDescending(z => z.DecisionDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (approvedEligibility is null)
            {
                return Result.Failure<CreateDistributionResponse>(
                    Error.Validation("Distributions.ZakatEligibilityRequired",
                        "This participant needs an approved, unexpired zakat eligibility case before a distribution can be recorded against the zakat program bucket."));
            }

            if (string.IsNullOrWhiteSpace(zakatAsnaf))
            {
                zakatAsnaf = approvedEligibility.AsnafCategory;
            }
            else if (!string.Equals(zakatAsnaf, approvedEligibility.AsnafCategory, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<CreateDistributionResponse>(
                    Error.Validation("Distributions.ZakatAsnafMismatch",
                        "The provided zakat asnaf does not match the participant's approved eligibility asnaf category."));
            }
        }

        // Server-computed, never client-supplied — BeneficiaryCount deliberately stays out of this
        // formula: ParticipantId is a single non-nullable FK, so "qty x unit cost x headcount"
        // (as the workflow spec frames a bulk distribution event) would double-count reach for a
        // per-participant row. BeneficiaryCount still flows to the generated Expense for zakat
        // asnaf reporting.
        var totalValuePhp = Math.Round(request.Quantity * request.UnitValuePhp, 2);

        Expense? expense = null;

        if (totalValuePhp > 0)
        {
            var paymentMethod = request.PaymentMethod
                ?? (string.Equals(request.DistributionType, "CASH_ASSISTANCE", StringComparison.OrdinalIgnoreCase) ? "CASH" : "IN_KIND");

            // Posted LAST, after every other check, per ExpensePosting.Post's own contract: it
            // debits the tracked bucket in-memory as an inseparable part of the balance check, so
            // nothing below this point may fail without also discarding this change (it isn't
            // saved yet — only SaveChangesAsync below commits it).
            var posting = ExpensePosting.Post(bucket, new ExpensePostingRequest(
                ExpenseDate: request.DistributionDate,
                PayeeVendor: $"Beneficiary #{participant.Id}", // not participant.FullName — Expense reads are open to any authenticated role, participant PII is not
                ExpenseCategory: FinanceRules.DistributionExpenseCategory,
                Description: $"{request.DistributionType} — qty {request.Quantity}",
                PaymentMethod: paymentMethod,
                AmountOriginal: totalValuePhp,
                Currency: "PHP",
                FxRateToPhp: 1m,
                ProgramOrProject: activity.Project.Name,
                ZakatAsnaf: zakatAsnaf,
                BeneficiaryCount: request.BeneficiaryCount));

            if (posting.IsFailure)
            {
                return Result.Failure<CreateDistributionResponse>(posting.Error);
            }

            expense = posting.Value;
        }

        var distribution = new Distribution
        {
            DistributionType = request.DistributionType,
            BeneficiaryId = participant.Id,
            ActivityId = activity.Id,
            FundingBucketCode = request.FundingBucketCode,
            Quantity = request.Quantity,
            UnitValuePhp = request.UnitValuePhp,
            TotalValuePhp = totalValuePhp,
            BeneficiaryCount = request.BeneficiaryCount,
            DistributionDate = request.DistributionDate,
            Location = request.Location,
            FieldVerified = request.FieldVerified,
            ReceivedConfirmation = request.ReceivedConfirmation,
            ProcessedBy = request.ProcessedBy,
            ZakatAsnaf = zakatAsnaf,
            Notes = request.Notes,
            IsVoided = false,
            Expense = expense // navigation, not ExpenseId — one SaveChangesAsync writes both rows
        };

        db.Distributions.Add(distribution);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateDistributionResponse(
            distribution.Id, distribution.DistributionType, distribution.BeneficiaryId, distribution.ActivityId!.Value,
            distribution.FundingBucketCode!, distribution.Quantity, distribution.UnitValuePhp, distribution.TotalValuePhp,
            distribution.BeneficiaryCount, distribution.DistributionDate, distribution.ZakatAsnaf, distribution.IsVoided,
            distribution.ExpenseId, bucket.Remaining));
    }
}
