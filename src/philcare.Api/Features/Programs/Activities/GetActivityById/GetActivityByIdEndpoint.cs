using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Activities.GetActivityById;

public sealed record ActivityDetailResponse(
    int Id,
    int ProjectId,
    string ProjectName,
    string Name,
    string? ActivityCategory,
    string ActivityType,
    string? TargetGroup,
    string? Barangay,
    string? City,
    string? Province,
    string? Region,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal Budget,
    string? ImplementingPartner,
    int? ImplementingPartnerId,
    string? ImplementingPartnerName,
    string? ResponsibleDepartment,
    string? SdgAlignment,
    string ImplementationStatus,
    string? SafeguardingRisk,
    string? EvidenceLink,
    string? Notes,
    bool IsActive,
    int ParticipantCount,
    int DistributionCount);

public sealed class GetActivityByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/activities/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var activity = await db.Activities
                .Where(a => a.Id == id)
                .Select(a => new ActivityDetailResponse(
                    a.Id, a.ProjectId, a.Project.Name, a.Name, a.ActivityCategory, a.ActivityType, a.TargetGroup,
                    a.Barangay, a.City, a.Province, a.Region, a.StartDate, a.EndDate, a.Budget, a.ImplementingPartner,
                    a.ImplementingPartnerId, a.ImplementingPartnerRef == null ? null : a.ImplementingPartnerRef.Name,
                    a.ResponsibleDepartment, a.SdgAlignment, a.ImplementationStatus, a.SafeguardingRisk, a.EvidenceLink,
                    a.Notes, a.IsActive, a.ActivityParticipants.Count, a.Distributions.Count))
                .FirstOrDefaultAsync(ct);

            if (activity is null)
            {
                return Results.Problem(title: "Activities.NotFound", detail: "Activity not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(activity);
        })
        .WithName("GetActivityById")
        .WithTags("Activities")
        .RequireAuthorization();
    }
}
