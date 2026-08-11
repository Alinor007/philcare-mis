using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.Roles.CreateGovernanceRole;

public sealed class CreateGovernanceRoleEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/governance/roles", async (CreateGovernanceRoleRequest request, CreateGovernanceRoleHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/governance/roles/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateGovernanceRole")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<CreateGovernanceRoleRequest>>()
        .RequireAuthorization("Admin");
    }
}
