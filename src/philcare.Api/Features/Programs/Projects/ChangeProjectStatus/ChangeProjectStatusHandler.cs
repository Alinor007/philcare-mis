using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Projects.ChangeProjectStatus;

public sealed class ChangeProjectStatusHandler(AppDbContext db)
{
    public async Task<Result<ChangeProjectStatusResponse>> HandleAsync(
        int id, ChangeProjectStatusRequest request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null)
        {
            return Result.Failure<ChangeProjectStatusResponse>(Error.NotFound("Projects.NotFound", "Project not found."));
        }

        if (!ImplementationStatusTransitions.CanTransition(project.ImplementationStatus, request.Status))
        {
            return Result.Failure<ChangeProjectStatusResponse>(
                Error.Validation("Projects.InvalidStatusTransition",
                    $"Cannot transition from {project.ImplementationStatus} to {request.Status}."));
        }

        var isClosing = string.Equals(request.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase);

        if (isClosing)
        {
            // Plain ordinal comparison — stored codes are always SCREAMING_SNAKE_CASE by convention
            // (same as GetActivities' filter). string.Equals(..., OrdinalIgnoreCase) cannot be
            // translated to SQL here since db.Activities is still an IQueryable at this point.
            var hasOpenActivities = await db.Activities.AnyAsync(
                a => a.ProjectId == project.Id && a.ImplementationStatus != "COMPLETED" && a.ImplementationStatus != "CANCELLED",
                cancellationToken);

            if (hasOpenActivities)
            {
                return Result.Failure<ChangeProjectStatusResponse>(
                    Error.Validation("Projects.HasOpenActivities",
                        "All activities must be Completed or Cancelled before the project can be closed."));
            }
        }

        project.ImplementationStatus = request.Status;

        if (isClosing)
        {
            project.ClosedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ChangeProjectStatusResponse(project.Id, project.ImplementationStatus, project.ClosedAt));
    }
}
