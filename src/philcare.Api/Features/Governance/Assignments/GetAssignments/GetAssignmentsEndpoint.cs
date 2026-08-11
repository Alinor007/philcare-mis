using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Assignments.GetAssignments;

public sealed record AssignmentListItemResponse(
    int Id, int PersonId, string PersonFullName, int OrgBodyId, string OrgBodyName, int GovernanceRoleId,
    string GovernanceRoleName, bool IsPrimary, string Status);

public sealed class GetAssignmentsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/assignments", async (
            int? personId, int? bodyId, int? roleId, AssignmentStatus? status, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Assignments
                .Include(a => a.Person)
                .Include(a => a.OrgBody)
                .Include(a => a.GovernanceRole)
                .AsQueryable();

            if (personId is not null)
            {
                query = query.Where(a => a.PersonId == personId);
            }

            if (bodyId is not null)
            {
                query = query.Where(a => a.OrgBodyId == bodyId);
            }

            if (roleId is not null)
            {
                query = query.Where(a => a.GovernanceRoleId == roleId);
            }

            if (status is not null)
            {
                query = query.Where(a => a.Status == status);
            }

            var assignments = await query
                .OrderByDescending(a => a.StartDate)
                .Select(a => new AssignmentListItemResponse(
                    a.Id, a.PersonId, a.Person.FullName, a.OrgBodyId, a.OrgBody.Name, a.GovernanceRoleId,
                    a.GovernanceRole.Name, a.IsPrimary, a.Status.ToString()))
                .ToListAsync(ct);

            return Results.Ok(assignments);
        })
        .WithName("GetAssignments")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
