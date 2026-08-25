using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.ProjectDonors.RemoveProjectDonor;

public sealed class RemoveProjectDonorEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/projects/{projectId:int}/donors/{donorId:int}", async (
            int projectId, int donorId, AppDbContext db, CancellationToken ct) =>
        {
            var link = await db.ProjectDonors
                .FirstOrDefaultAsync(pd => pd.ProjectId == projectId && pd.DonorId == donorId, ct);

            if (link is null)
            {
                return Results.Problem(
                    title: "ProjectDonors.NotFound",
                    detail: "This donor is not linked to this project.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            // Hard delete — a plain link with no receipt/evidence data worth preserving, unlike
            // the distribution reach roster (see ProjectDonor's class doc).
            db.ProjectDonors.Remove(link);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("RemoveProjectDonor")
        .WithTags("Projects")
        .RequireAuthorization("Program");
    }
}
