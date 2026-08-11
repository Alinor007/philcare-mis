using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Activities.ChangeActivityStatus;

public sealed class ChangeActivityStatusHandler(AppDbContext db)
{
    public async Task<Result<ChangeActivityStatusResponse>> HandleAsync(
        int id, ChangeActivityStatusRequest request, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (activity is null)
        {
            return Result.Failure<ChangeActivityStatusResponse>(Error.NotFound("Activities.NotFound", "Activity not found."));
        }

        if (!ImplementationStatusTransitions.CanTransition(activity.ImplementationStatus, request.Status))
        {
            return Result.Failure<ChangeActivityStatusResponse>(
                Error.Validation("Activities.InvalidStatusTransition",
                    $"Cannot transition from {activity.ImplementationStatus} to {request.Status}."));
        }

        activity.ImplementationStatus = request.Status;

        if (string.Equals(request.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            activity.ActualBeneficiaries = request.ActualBeneficiaries;
            activity.ActualEndDate = request.ActualEndDate ?? DateTime.UtcNow.Date;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ChangeActivityStatusResponse(
            activity.Id, activity.ImplementationStatus, activity.ActualBeneficiaries, activity.ActualEndDate));
    }
}
