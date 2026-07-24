using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Activities.CreateActivity;

public sealed class CreateActivityHandler(AppDbContext db)
{
    public async Task<Result<CreateActivityResponse>> HandleAsync(CreateActivityRequest request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<CreateActivityResponse>(Error.NotFound("Activities.ProjectNotFound", "Project not found."));
        }

        if (!project.IsActive)
        {
            return Result.Failure<CreateActivityResponse>(
                Error.Validation("Activities.ProjectInactive", "Cannot create an activity under an inactive project."));
        }

        var activity = new Activity
        {
            ProjectId = project.Id,
            Name = request.Name,
            ActivityCategory = request.ActivityCategory,
            ActivityType = request.ActivityType,
            TargetGroup = request.TargetGroup,
            Barangay = request.Barangay,
            City = request.City,
            Province = request.Province,
            Region = request.Region,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Budget = request.Budget,
            ImplementingPartner = request.ImplementingPartner,
            ResponsibleDepartment = request.ResponsibleDepartment,
            SdgAlignment = request.SdgAlignment,
            ImplementationStatus = "PLANNED",
            SafeguardingRisk = request.SafeguardingRisk,
            EvidenceLink = request.EvidenceLink,
            Notes = request.Notes,
            IsActive = true
        };

        db.Activities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateActivityResponse(
            activity.Id, activity.ProjectId, activity.Name, activity.ActivityCategory, activity.ActivityType,
            activity.TargetGroup, activity.Budget, activity.ImplementationStatus, activity.IsActive));
    }
}
