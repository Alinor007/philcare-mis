using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.OrgBodies.GetOrgBodies;

public sealed record OrgBodyListItemResponse(int Id, string Name, string BodyType, int? ParentBodyId, string? ParentBodyName, bool IsActive);

public sealed class GetOrgBodiesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/bodies", async (string? bodyType, bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.OrgBodies.Include(b => b.ParentBody).AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(b => b.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(bodyType))
            {
                query = query.Where(b => b.BodyType == bodyType);
            }

            var bodies = await query
                .OrderBy(b => b.Name)
                .Select(b => new OrgBodyListItemResponse(b.Id, b.Name, b.BodyType, b.ParentBodyId, b.ParentBody == null ? null : b.ParentBody.Name, b.IsActive))
                .ToListAsync(ct);

            return Results.Ok(bodies);
        })
        .WithName("GetOrgBodies")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
