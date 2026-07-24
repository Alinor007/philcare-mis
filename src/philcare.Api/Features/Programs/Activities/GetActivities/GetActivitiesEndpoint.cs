using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Activities.GetActivities;

public sealed record ActivityListItemResponse(
    int Id, int ProjectId, string ProjectName, string Name, string ActivityType, decimal Budget, string ImplementationStatus, bool IsActive);

public sealed class GetActivitiesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/activities", async (
            int? projectId,
            string? implementationStatus,
            DateTime? from,
            DateTime? to,
            bool? includeInactive,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var query = db.Activities.Include(a => a.Project).AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(a => a.IsActive);
            }

            if (projectId is not null)
            {
                query = query.Where(a => a.ProjectId == projectId);
            }

            if (!string.IsNullOrWhiteSpace(implementationStatus))
            {
                query = query.Where(a => a.ImplementationStatus == implementationStatus);
            }

            if (from is not null)
            {
                query = query.Where(a => a.StartDate == null || a.StartDate >= from);
            }

            if (to is not null)
            {
                query = query.Where(a => a.StartDate == null || a.StartDate <= to);
            }

            var activities = await query
                .OrderBy(a => a.Name)
                .Select(a => new ActivityListItemResponse(a.Id, a.ProjectId, a.Project.Name, a.Name, a.ActivityType, a.Budget, a.ImplementationStatus, a.IsActive))
                .ToListAsync(ct);

            return Results.Ok(activities);
        })
        .WithName("GetActivities")
        .WithTags("Activities")
        .RequireAuthorization();
    }
}
