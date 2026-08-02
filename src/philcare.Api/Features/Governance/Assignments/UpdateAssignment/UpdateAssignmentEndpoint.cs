using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.Assignments.UpdateAssignment;

public sealed class UpdateAssignmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/governance/assignments/{id:int}", async (int id, UpdateAssignmentRequest request, UpdateAssignmentHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateAssignment")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<UpdateAssignmentRequest>>()
        .RequireAuthorization("Admin");
    }
}
