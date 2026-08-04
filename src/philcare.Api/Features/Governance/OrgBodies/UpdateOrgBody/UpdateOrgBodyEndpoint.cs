using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.OrgBodies.UpdateOrgBody;

public sealed class UpdateOrgBodyEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/governance/bodies/{id:int}", async (int id, UpdateOrgBodyRequest request, UpdateOrgBodyHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateOrgBody")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<UpdateOrgBodyRequest>>()
        .RequireAuthorization("Admin");
    }
}
