using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.Roles.GetGovernanceRoleById;

public sealed record GovernanceRoleDetailResponse(
    int Id,
    string Name,
    string RoleCategory,
    int? DefaultBodyId,
    string? DefaultBodyName,
    string? DefaultVotingRights,
    string? CountsForQuorum,
    string? Delegable,
    string? Notes,
    bool IsActive);

public sealed class GetGovernanceRoleByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/roles/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var role = await db.GovernanceRoles
                .Where(r => r.Id == id)
                .Select(r => new GovernanceRoleDetailResponse(
                    r.Id, r.Name, r.RoleCategory, r.DefaultBodyId, r.DefaultBody == null ? null : r.DefaultBody.Name,
                    r.DefaultVotingRights, r.CountsForQuorum, r.Delegable, r.Notes, r.IsActive))
                .FirstOrDefaultAsync(ct);

            if (role is null)
            {
                return Results.Problem(title: "Governance.RoleNotFound", detail: "Governance role not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(role);
        })
        .WithName("GetGovernanceRoleById")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
