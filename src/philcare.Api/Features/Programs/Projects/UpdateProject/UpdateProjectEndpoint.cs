using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.Projects.UpdateProject;

public sealed class UpdateProjectEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/projects/{id:int}", async (int id, UpdateProjectRequest request, UpdateProjectHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateProject")
        .WithTags("Projects")
        .AddEndpointFilter<ValidationFilter<UpdateProjectRequest>>()
        .RequireAuthorization("Program");
    }
}
