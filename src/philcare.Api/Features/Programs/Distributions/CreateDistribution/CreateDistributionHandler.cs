using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Distributions.CreateDistribution;

public sealed class CreateDistributionHandler(AppDbContext db)
{
    public async Task<Result<CreateDistributionResponse>> HandleAsync(CreateDistributionRequest request, CancellationToken cancellationToken)
    {
        var participant = await db.Participants.FirstOrDefaultAsync(p => p.Id == request.ParticipantId, cancellationToken);

        if (participant is null)
        {
            return Result.Failure<CreateDistributionResponse>(Error.NotFound("Distributions.ParticipantNotFound", "Participant not found."));
        }

        if (!participant.IsActive)
        {
            return Result.Failure<CreateDistributionResponse>(
                Error.Validation("Distributions.ParticipantInactive", "Cannot record a distribution for an inactive participant."));
        }

        if (request.ActivityId is not null)
        {
            var activityExists = await db.Activities.AnyAsync(a => a.Id == request.ActivityId, cancellationToken);
            if (!activityExists)
            {
                return Result.Failure<CreateDistributionResponse>(Error.NotFound("Distributions.ActivityNotFound", "Activity not found."));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.FundingBucketCode))
        {
            var bucket = await db.FundingBuckets.FirstOrDefaultAsync(b => b.Code == request.FundingBucketCode, cancellationToken);

            if (bucket is null)
            {
                return Result.Failure<CreateDistributionResponse>(Error.NotFound("Distributions.FundingBucketNotFound", "Funding bucket not found."));
            }

            var isZakatProgramBucket = string.Equals(bucket.FundCode, FinanceRules.ZakatFundCode, StringComparison.OrdinalIgnoreCase)
                && bucket.BucketType == BucketType.Program;

            if (isZakatProgramBucket && string.IsNullOrWhiteSpace(request.ZakatAsnaf))
            {
                return Result.Failure<CreateDistributionResponse>(
                    Error.Validation("Distributions.ZakatAsnafRequired", "Zakat asnaf is required for distributions against the zakat program bucket."));
            }
        }

        var distribution = new Distribution
        {
            DistributionType = request.DistributionType,
            ParticipantId = participant.Id,
            ActivityId = request.ActivityId,
            FundingBucketCode = request.FundingBucketCode,
            Quantity = request.Quantity,
            TotalValuePhp = request.TotalValuePhp,
            DistributionDate = request.DistributionDate,
            Location = request.Location,
            FieldVerified = request.FieldVerified,
            ReceivedConfirmation = request.ReceivedConfirmation,
            ProcessedBy = request.ProcessedBy,
            ZakatAsnaf = request.ZakatAsnaf,
            Notes = request.Notes,
            IsVoided = false
        };

        db.Distributions.Add(distribution);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateDistributionResponse(
            distribution.Id, distribution.DistributionType, distribution.ParticipantId, distribution.ActivityId,
            distribution.FundingBucketCode, distribution.Quantity, distribution.TotalValuePhp, distribution.DistributionDate,
            distribution.IsVoided));
    }
}
