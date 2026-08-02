using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.OrgBodies.DeactivateOrgBody;

public sealed class DeactivateOrgBodyEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/governance/bodies/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var body = await db.OrgBodies.FirstOrDefaultAsync(b => b.Id == id, ct);

            if (body is null)
            {
                return Results.Problem(title: "Governance.BodyNotFound", detail: "Governance body not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var hasActiveChildren = await db.OrgBodies.AnyAsync(b => b.ParentBodyId == id && b.IsActive, ct);
            var hasCurrentAssignments = await db.Assignments.AnyAsync(a => a.OrgBodyId == id && a.Status == AssignmentStatus.Current, ct);

            if (hasActiveChildren || hasCurrentAssignments)
            {
                return Results.Problem(
                    title: "Governance.BodyInUse",
                    detail: "Cannot deactivate a governance body that has active child bodies or current assignments.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            body.IsActive = false;
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeactivateOrgBody")
        .WithTags("Governance")
        .RequireAuthorization("Admin");
    }
}
