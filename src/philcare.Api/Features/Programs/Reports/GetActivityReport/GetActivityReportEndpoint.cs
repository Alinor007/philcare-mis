using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Reports.GetActivityReport;

public sealed record ActivityReportRow(
    int ActivityId,
    string ActivityName,
    int ProjectId,
    string ProjectName,
    string ActivityType,
    string ImplementationStatus,
    int? ActualBeneficiaries,
    DateTime? ActualEndDate,
    int ParticipantCount,
    int PresentCount,
    int DistributionCount,
    decimal TotalDistributedValuePhp);

/// <summary>
/// Workflow Phase 09 "Activity Report — distributions, attendance". One row per activity.
///
/// ParticipantCount/PresentCount describe the **staffing** roster — how many staff were assigned
/// and how many turned up — since ActivityParticipant now links Activity to StaffMember.
/// Beneficiary reach is DistributionCount / ActualBeneficiaries, not these two.
///
/// PresentCount reads the attendance_status lookup code directly (see LookupCategory.AttendanceStatus)
/// rather than a separate attendance report, since that vocabulary is enforced on assignment.
/// </summary>
public sealed class GetActivityReportEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/activity-report", async (int? projectId, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Activities.AsQueryable();

            if (projectId is not null)
            {
                query = query.Where(a => a.ProjectId == projectId);
            }

            var rows = await query
                .OrderBy(a => a.Name)
                .Select(a => new ActivityReportRow(
                    a.Id,
                    a.Name,
                    a.ProjectId,
                    a.Project.Name,
                    a.ActivityType,
                    a.ImplementationStatus,
                    a.ActualBeneficiaries,
                    a.ActualEndDate,
                    a.ActivityParticipants.Count(ap => ap.IsActive),
                    a.ActivityParticipants.Count(ap => ap.IsActive && ap.AttendanceStatus == "PRESENT"),
                    a.Distributions.Count(d => !d.IsVoided),
                    a.Distributions.Where(d => !d.IsVoided).Sum(d => d.TotalValuePhp)))
                .ToListAsync(ct);

            return Results.Ok(rows);
        })
        .WithName("GetActivityReport")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
