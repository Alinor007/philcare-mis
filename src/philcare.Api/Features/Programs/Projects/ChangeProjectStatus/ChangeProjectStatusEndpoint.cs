using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.Projects.ChangeProjectStatus;

public sealed class ChangeProjectStatusEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{id:int}/status", async (
            int id, ChangeProjectStatusRequest request, ChangeProjectStatusHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("ChangeProjectStatus")
        .WithTags("Projects")
        .AddEndpointFilter<ValidationFilter<ChangeProjectStatusRequest>>()
        .RequireAuthorization("Program");
    }
}
