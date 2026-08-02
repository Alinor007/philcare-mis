using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.Finance.DonorEngagements.CreateDonorEngagement;

public sealed class CreateDonorEngagementHandler(AppDbContext db)
{
    public async Task<Result<CreateDonorEngagementResponse>> HandleAsync(CreateDonorEngagementRequest request, CancellationToken cancellationToken)
    {
        var donorExists = await db.Donors.AnyAsync(d => d.Id == request.DonorId, cancellationToken);

        if (!donorExists)
        {
            return Result.Failure<CreateDonorEngagementResponse>(Error.NotFound("DonorEngagements.DonorNotFound", "Donor not found."));
        }

        var validEngagementType = await db.LookupItems.AnyAsync(
            l => l.Category == LookupCategory.EngagementType && l.Code == request.EngagementType && l.IsActive, cancellationToken);

        if (!validEngagementType)
        {
            return Result.Failure<CreateDonorEngagementResponse>(
                Error.Validation("DonorEngagements.InvalidEngagementType", "Engagement type must be a valid engagement_type lookup code."));
        }

        var engagement = new DonorEngagement
        {
            DonorId = request.DonorId,
            EngagementType = request.EngagementType,
            EngagementDate = request.EngagementDate,
            Subject = request.Subject,
            Notes = request.Notes,
            FollowUpRequired = request.FollowUpRequired,
            FollowUpDate = request.FollowUpDate
        };

        db.DonorEngagements.Add(engagement);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateDonorEngagementResponse(
            engagement.Id, engagement.DonorId, engagement.EngagementType, engagement.EngagementDate, engagement.Subject,
            engagement.Notes, engagement.FollowUpRequired, engagement.FollowUpDate, engagement.CreatedBy, engagement.CreatedAt));
    }
}
