using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.OrgBodies.GetOrgBodyById;

public sealed record ChildBodyRow(int Id, string Name, string BodyType);

public sealed record OrgBodyDetailResponse(
    int Id,
    string Name,
    string BodyType,
    int? ParentBodyId,
    string? ParentBodyName,
    string? QuorumRule,
    string? DecisionThreshold,
    string? MeetingFrequency,
    string? PolicyBasis,
    string? Notes,
    bool IsActive,
    int CurrentMemberCount,
    List<ChildBodyRow> ChildBodies);

public sealed class GetOrgBodyByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/bodies/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var body = await db.OrgBodies
                .Where(b => b.Id == id)
                .Select(b => new OrgBodyDetailResponse(
                    b.Id, b.Name, b.BodyType, b.ParentBodyId, b.ParentBody == null ? null : b.ParentBody.Name,
                    b.QuorumRule, b.DecisionThreshold, b.MeetingFrequency, b.PolicyBasis, b.Notes, b.IsActive,
                    b.Assignments.Count(a => a.Status == AssignmentStatus.Current),
                    b.ChildBodies.Select(c => new ChildBodyRow(c.Id, c.Name, c.BodyType)).ToList()))
                .FirstOrDefaultAsync(ct);

            if (body is null)
            {
                return Results.Problem(title: "Governance.BodyNotFound", detail: "Governance body not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(body);
        })
        .WithName("GetOrgBodyById")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
