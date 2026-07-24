using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Projects.CreateProject;

public sealed class CreateProjectHandler(AppDbContext db)
{
    public async Task<Result<CreateProjectResponse>> HandleAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var program = await db.Programs.FirstOrDefaultAsync(p => p.Id == request.ProgramId, cancellationToken);

        if (program is null)
        {
            return Result.Failure<CreateProjectResponse>(Error.NotFound("Projects.ProgramNotFound", "Program not found."));
        }

        if (!program.IsActive)
        {
            return Result.Failure<CreateProjectResponse>(
                Error.Validation("Projects.ProgramInactive", "Cannot create a project under an inactive program."));
        }

        var project = new Project
        {
            ProgramId = program.Id,
            Name = request.Name,
            DonorId = request.DonorId,
            FundCode = request.FundCode,
            TotalBudget = request.TotalBudget,
            TargetBeneficiaries = request.TargetBeneficiaries,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Location = request.Location,
            ProjectManager = request.ProjectManager,
            ImplementationStatus = "PLANNED",
            ApprovalLevel = request.ApprovalLevel,
            Notes = request.Notes,
            IsActive = true
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateProjectResponse(
            project.Id, project.ProgramId, project.Name, project.DonorId, project.FundCode, project.TotalBudget,
            project.TargetBeneficiaries, project.StartDate, project.EndDate, project.Location, project.ProjectManager,
            project.ImplementationStatus, project.ApprovalLevel, project.Notes, project.IsActive));
    }
}
