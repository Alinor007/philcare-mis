using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.ProjectDonors.AddProjectDonor;

/// <summary>Links a donor to a project. Plain link — no consent/eligibility gate, unlike the
/// distribution reach roster, since this moves no money and grants no aid.</summary>
public sealed class AddProjectDonorHandler(AppDbContext db)
{
    public async Task<Result<AddProjectDonorResponse>> HandleAsync(
        int projectId, AddProjectDonorRequest request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<AddProjectDonorResponse>(
                Error.NotFound("ProjectDonors.ProjectNotFound", "Project not found."));
        }

        var donor = await db.Donors.FirstOrDefaultAsync(d => d.Id == request.DonorId, cancellationToken);

        if (donor is null)
        {
            return Result.Failure<AddProjectDonorResponse>(
                Error.NotFound("ProjectDonors.DonorNotFound", "Donor not found."));
        }

        if (!donor.IsActive)
        {
            return Result.Failure<AddProjectDonorResponse>(
                Error.Validation("ProjectDonors.DonorInactive", "Cannot link an inactive donor to a project."));
        }

        var alreadyLinked = await db.ProjectDonors
            .AnyAsync(pd => pd.ProjectId == projectId && pd.DonorId == request.DonorId, cancellationToken);

        if (alreadyLinked)
        {
            return Result.Failure<AddProjectDonorResponse>(
                Error.Conflict("ProjectDonors.AlreadyLinked", "This donor is already linked to this project."));
        }

        var link = new ProjectDonor { ProjectId = projectId, DonorId = request.DonorId };
        db.ProjectDonors.Add(link);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new AddProjectDonorResponse(link.Id, link.ProjectId, link.DonorId, donor.Name));
    }
}
