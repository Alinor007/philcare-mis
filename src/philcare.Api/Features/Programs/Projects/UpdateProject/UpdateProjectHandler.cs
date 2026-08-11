using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Projects.UpdateProject;

public sealed class UpdateProjectHandler(AppDbContext db)
{
    public async Task<Result<UpdateProjectResponse>> HandleAsync(int id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null)
        {
            return Result.Failure<UpdateProjectResponse>(Error.NotFound("Projects.NotFound", "Project not found."));
        }

        project.Name = request.Name;
        project.DonorId = request.DonorId;
        project.FundCode = request.FundCode;
        project.TotalBudget = request.TotalBudget;
        project.TargetBeneficiaries = request.TargetBeneficiaries;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Location = request.Location;
        project.ProjectManager = request.ProjectManager;
        // ImplementationStatus is intentionally not settable here — status changes go through
        // POST /api/projects/{id}/status, which enforces the transition table and the
        // Projects.HasOpenActivities closeout guard.
        project.ApprovalLevel = request.ApprovalLevel;
        project.Notes = request.Notes;
        project.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateProjectResponse(
            project.Id, project.ProgramId, project.Name, project.DonorId, project.FundCode, project.TotalBudget,
            project.TargetBeneficiaries, project.StartDate, project.EndDate, project.Location, project.ProjectManager,
            project.ImplementationStatus, project.ApprovalLevel, project.Notes, project.IsActive));
    }
}
