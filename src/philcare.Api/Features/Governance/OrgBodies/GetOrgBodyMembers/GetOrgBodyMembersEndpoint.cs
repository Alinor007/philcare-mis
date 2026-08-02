using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.OrgBodies.GetOrgBodyMembers;

public sealed record OrgBodyMemberRow(
    int PersonId, string PersonFullName, int AssignmentId, string RoleName, string? PositionTitle,
    bool IsPrimary, bool VotingRights, DateTime StartDate, DateTime? EndDate, string Status);

public sealed class GetOrgBodyMembersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        // The board/executive "roster" is not a stored entity — it is derived from Assignments.
        // By default only Current assignments are returned (today's roster); includeFormer=true
        // or asOf=<date> widen the view for historical lookups.
        app.MapGet("/api/governance/bodies/{id:int}/members", async (
            int id, DateTime? asOf, bool? includeFormer, AppDbContext db, CancellationToken ct) =>
        {
            var bodyExists = await db.OrgBodies.AnyAsync(b => b.Id == id, ct);

            if (!bodyExists)
            {
                return Results.Problem(title: "Governance.BodyNotFound", detail: "Governance body not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var query = db.Assignments
                .Include(a => a.Person)
                .Include(a => a.GovernanceRole)
                .Where(a => a.OrgBodyId == id)
                .AsQueryable();

            if (asOf is not null)
            {
                var asOfDate = asOf.Value;
                query = query.Where(a => a.StartDate <= asOfDate && (a.EndDate == null || a.EndDate >= asOfDate));
            }
            else if (includeFormer != true)
            {
                query = query.Where(a => a.Status == AssignmentStatus.Current);
            }

            var members = await query
                .OrderByDescending(a => a.IsPrimary)
                .ThenBy(a => a.Person.FullName)
                .Select(a => new OrgBodyMemberRow(
                    a.PersonId, a.Person.FullName, a.Id, a.GovernanceRole.Name, a.PositionTitle,
                    a.IsPrimary, a.VotingRights, a.StartDate, a.EndDate, a.Status.ToString()))
                .ToListAsync(ct);

            return Results.Ok(members);
        })
        .WithName("GetOrgBodyMembers")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
