using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.Assignments.GetAssignmentById;

public sealed record AssignmentDetailResponse(
    int Id,
    int PersonId,
    string PersonFullName,
    int OrgBodyId,
    string OrgBodyName,
    int GovernanceRoleId,
    string GovernanceRoleName,
    string? PositionTitle,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsPrimary,
    bool VotingRights,
    bool IsTemporary,
    string Status,
    string? Notes);

public sealed class GetAssignmentByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/assignments/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var assignment = await db.Assignments
                .Where(a => a.Id == id)
                .Select(a => new AssignmentDetailResponse(
                    a.Id, a.PersonId, a.Person.FullName, a.OrgBodyId, a.OrgBody.Name, a.GovernanceRoleId, a.GovernanceRole.Name,
                    a.PositionTitle, a.StartDate, a.EndDate, a.IsPrimary, a.VotingRights, a.IsTemporary, a.Status.ToString(), a.Notes))
                .FirstOrDefaultAsync(ct);

            if (assignment is null)
            {
                return Results.Problem(title: "Governance.AssignmentNotFound", detail: "Assignment not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(assignment);
        })
        .WithName("GetAssignmentById")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
