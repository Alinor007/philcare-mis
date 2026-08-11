using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.Roles.GetGovernanceRoles;

public sealed record GovernanceRoleListItemResponse(int Id, string Name, string RoleCategory, bool IsActive);

public sealed class GetGovernanceRolesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/roles", async (string? roleCategory, bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.GovernanceRoles.AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(r => r.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(roleCategory))
            {
                query = query.Where(r => r.RoleCategory == roleCategory);
            }

            var roles = await query
                .OrderBy(r => r.Name)
                .Select(r => new GovernanceRoleListItemResponse(r.Id, r.Name, r.RoleCategory, r.IsActive))
                .ToListAsync(ct);

            return Results.Ok(roles);
        })
        .WithName("GetGovernanceRoles")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
