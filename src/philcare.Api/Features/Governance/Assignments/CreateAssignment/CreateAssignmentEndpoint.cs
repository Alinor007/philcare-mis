using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.Assignments.CreateAssignment;

public sealed class CreateAssignmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/governance/assignments", async (CreateAssignmentRequest request, CreateAssignmentHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/governance/assignments/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateAssignment")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<CreateAssignmentRequest>>()
        .RequireAuthorization("Admin");
    }
}
