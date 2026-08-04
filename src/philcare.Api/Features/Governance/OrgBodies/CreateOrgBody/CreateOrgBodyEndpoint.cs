using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.OrgBodies.CreateOrgBody;

public sealed class CreateOrgBodyEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/governance/bodies", async (CreateOrgBodyRequest request, CreateOrgBodyHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/governance/bodies/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateOrgBody")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<CreateOrgBodyRequest>>()
        .RequireAuthorization("Admin");
    }
}
