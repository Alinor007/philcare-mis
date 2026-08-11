using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.Decisions.CreateDecision;

public sealed class CreateDecisionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/governance/minutes/{minutesId:int}/decisions", async (
            int minutesId, CreateDecisionRequest request, CreateDecisionHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(minutesId, request, ct);
            return result.IsSuccess ? Results.Created($"/api/governance/minutes/{minutesId}/decisions", result.Value) : result.ToProblem();
        })
        .WithName("CreateDecision")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<CreateDecisionRequest>>()
        .RequireAuthorization("Admin");
    }
}
