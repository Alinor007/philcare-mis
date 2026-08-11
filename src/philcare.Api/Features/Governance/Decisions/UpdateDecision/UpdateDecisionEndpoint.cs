using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.Decisions.UpdateDecision;

public sealed class UpdateDecisionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/governance/decisions/{id:int}", async (int id, UpdateDecisionRequest request, UpdateDecisionHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateDecision")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<UpdateDecisionRequest>>()
        .RequireAuthorization("Admin");
    }
}
