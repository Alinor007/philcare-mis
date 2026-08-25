using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.ProjectDonors.GetProjectDonors;

public sealed record ProjectDonorRow(int DonorId, string DonorName, string DonorType, bool IsActive);

public sealed class GetProjectDonorsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:int}/donors", async (int projectId, AppDbContext db, CancellationToken ct) =>
        {
            var projectExists = await db.Projects.AnyAsync(p => p.Id == projectId, ct);

            if (!projectExists)
            {
                return Results.Problem(
                    title: "Projects.NotFound", detail: "Project not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var donors = await db.ProjectDonors
                .Where(pd => pd.ProjectId == projectId)
                .Include(pd => pd.Donor)
                .OrderBy(pd => pd.Donor.Name)
                .Select(pd => new ProjectDonorRow(pd.DonorId, pd.Donor.Name, pd.Donor.Type.ToString(), pd.Donor.IsActive))
                .ToListAsync(ct);

            return Results.Ok(donors);
        })
        .WithName("GetProjectDonors")
        .WithTags("Projects")
        .RequireAuthorization();
    }
}
