using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Distributions.CreateDistribution;

/// <summary>
/// Creates the handout event and books the money. It does NOT record who received anything —
/// that is the roster's job (POST /api/distributions/{id}/beneficiaries), which is also where the
/// consent and zakat-eligibility gates now live. A new distribution therefore starts at zero reach.
/// </summary>
public sealed class CreateDistributionHandler(AppDbContext db)
{
    public async Task<Result<CreateDistributionResponse>> HandleAsync(CreateDistributionRequest request, CancellationToken cancellationToken)
    {
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

        // Asnaf used to be derived from the single recipient's approved eligibility. With no
        // recipient at creation there is nothing to derive it from, so the officer supplies it and
        // it becomes the category every roster member must match (AddDistributionBeneficiaryHandler
        // enforces that). ExpensePosting would reject a blank one anyway; failing here gives a
        // message about the distribution rather than about the ledger.
        if (ExpensePosting.IsZakatProgramBucket(bucket) && string.IsNullOrWhiteSpace(request.ZakatAsnaf))
        {
            return Result.Failure<CreateDistributionResponse>(
                Error.Validation("Distributions.ZakatAsnafRequired",
                    "Zakat asnaf is required for a distribution paid from the zakat program bucket. Only beneficiaries approved under this asnaf can then be added."));
        }

        // Server-computed, never client-supplied. Reach is deliberately absent from this formula:
        // adding people to the roster records who was reached, it never changes what was spent.
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
                // The activity, not a beneficiary name: Expense reads are open to any authenticated
                // role and beneficiary PII is not. There is also no single recipient to name now.
                PayeeVendor: $"Distribution — {activity.Name}",
                ExpenseCategory: FinanceRules.DistributionExpenseCategory,
                Description: $"{request.DistributionType} — qty {request.Quantity}",
                PaymentMethod: paymentMethod,
                AmountOriginal: totalValuePhp,
                Currency: "PHP",
                FxRateToPhp: 1m,
                ProgramOrProject: activity.Project.Name,
                ZakatAsnaf: request.ZakatAsnaf,
                // Zero at posting time and corrected by DistributionReach.Sync as the roster fills.
                BeneficiaryCount: 0));

            if (posting.IsFailure)
            {
                return Result.Failure<CreateDistributionResponse>(posting.Error);
            }

            expense = posting.Value;
        }

        var distribution = new Distribution
        {
            DistributionType = request.DistributionType,
            ActivityId = activity.Id,
            FundingBucketCode = request.FundingBucketCode,
            Quantity = request.Quantity,
            UnitValuePhp = request.UnitValuePhp,
            TotalValuePhp = totalValuePhp,
            // Nobody has been recorded as reached yet; the roster drives this from here on.
            BeneficiaryCount = 0,
            DistributionDate = request.DistributionDate,
            Location = request.Location,
            FieldVerified = request.FieldVerified,
            ReceivedConfirmation = request.ReceivedConfirmation,
            ProcessedBy = request.ProcessedBy,
            ZakatAsnaf = request.ZakatAsnaf,
            Notes = request.Notes,
            IsVoided = false,
            Expense = expense // navigation, not ExpenseId — one SaveChangesAsync writes both rows
        };

        db.Distributions.Add(distribution);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateDistributionResponse(
            distribution.Id, distribution.DistributionType, distribution.ActivityId!.Value,
            distribution.FundingBucketCode!, distribution.Quantity, distribution.UnitValuePhp, distribution.TotalValuePhp,
            distribution.BeneficiaryCount, distribution.DistributionDate, distribution.ZakatAsnaf, distribution.IsVoided,
            distribution.ExpenseId, bucket.Remaining));
    }
}
