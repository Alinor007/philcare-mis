using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.Finance.DonorEngagements.UpdateDonorEngagement;

public sealed class UpdateDonorEngagementHandler(AppDbContext db)
{
    public async Task<Result<UpdateDonorEngagementResponse>> HandleAsync(int id, UpdateDonorEngagementRequest request, CancellationToken cancellationToken)
    {
        var engagement = await db.DonorEngagements.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (engagement is null)
        {
            return Result.Failure<UpdateDonorEngagementResponse>(Error.NotFound("DonorEngagements.NotFound", "Donor engagement not found."));
        }

        var validEngagementType = await db.LookupItems.AnyAsync(
            l => l.Category == LookupCategory.EngagementType && l.Code == request.EngagementType && l.IsActive, cancellationToken);

        if (!validEngagementType)
        {
            return Result.Failure<UpdateDonorEngagementResponse>(
                Error.Validation("DonorEngagements.InvalidEngagementType", "Engagement type must be a valid engagement_type lookup code."));
        }

        engagement.EngagementType = request.EngagementType;
        engagement.EngagementDate = request.EngagementDate;
        engagement.Subject = request.Subject;
        engagement.Notes = request.Notes;
        engagement.FollowUpRequired = request.FollowUpRequired;
        engagement.FollowUpDate = request.FollowUpDate;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateDonorEngagementResponse(
            engagement.Id, engagement.DonorId, engagement.EngagementType, engagement.EngagementDate, engagement.Subject,
            engagement.Notes, engagement.FollowUpRequired, engagement.FollowUpDate, engagement.CreatedBy, engagement.CreatedAt));
    }
}
