using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Activities.UpdateActivity;

public sealed class UpdateActivityHandler(AppDbContext db)
{
    public async Task<Result<UpdateActivityResponse>> HandleAsync(int id, UpdateActivityRequest request, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (activity is null)
        {
            return Result.Failure<UpdateActivityResponse>(Error.NotFound("Activities.NotFound", "Activity not found."));
        }

        activity.Name = request.Name;
        activity.ActivityCategory = request.ActivityCategory;
        activity.ActivityType = request.ActivityType;
        activity.TargetGroup = request.TargetGroup;
        activity.Barangay = request.Barangay;
        activity.City = request.City;
        activity.Province = request.Province;
        activity.Region = request.Region;
        activity.StartDate = request.StartDate;
        activity.EndDate = request.EndDate;
        activity.Budget = request.Budget;
        activity.ImplementingPartner = request.ImplementingPartner;
        activity.ResponsibleDepartment = request.ResponsibleDepartment;
        activity.SdgAlignment = request.SdgAlignment;
        activity.ImplementationStatus = request.ImplementationStatus;
        activity.SafeguardingRisk = request.SafeguardingRisk;
        activity.EvidenceLink = request.EvidenceLink;
        activity.Notes = request.Notes;
        activity.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateActivityResponse(
            activity.Id, activity.ProjectId, activity.Name, activity.ActivityType, activity.Budget, activity.ImplementationStatus, activity.IsActive));
    }
}
