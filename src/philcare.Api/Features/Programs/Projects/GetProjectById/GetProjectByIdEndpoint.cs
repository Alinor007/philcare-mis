using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Projects.GetProjectById;

public sealed record ProjectDetailResponse(
    int Id,
    int ProgramId,
    string ProgramName,
    string Name,
    string? FundType,
    decimal TotalBudget,
    int? TargetBeneficiaries,
    DateTime? StartDate,
    DateTime? EndDate,
    string? Location,
    string? ProjectManager,
    string ImplementationStatus,
    string? ApprovalLevel,
    string? Notes,
    bool IsActive,
    int ActivityCount,
    int DonorCount,
    // Set only by ChangeProjectStatus on the transition to COMPLETED — the UI reads it to show
    // when a project was closed, and to distinguish "closed" from merely "not ongoing".
    DateTime? ClosedAt);

public sealed class GetProjectByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects
                .Where(p => p.Id == id)
                .Select(p => new ProjectDetailResponse(
                    p.Id, p.ProgramId, p.Program.Name, p.Name, p.FundType, p.TotalBudget, p.TargetBeneficiaries,
                    p.StartDate, p.EndDate, p.Location, p.ProjectManager, p.ImplementationStatus, p.ApprovalLevel, p.Notes,
                    p.IsActive, p.Activities.Count, p.Donors.Count, p.ClosedAt))
                .FirstOrDefaultAsync(ct);

            if (project is null)
            {
                return Results.Problem(title: "Projects.NotFound", detail: "Project not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(project);
        })
        .WithName("GetProjectById")
        .WithTags("Projects")
        .RequireAuthorization();
    }
}
