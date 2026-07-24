using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.Projects.CreateProject;

public sealed class CreateProjectEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects", async (CreateProjectRequest request, CreateProjectHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/projects/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateProject")
        .WithTags("Projects")
        .AddEndpointFilter<ValidationFilter<CreateProjectRequest>>()
        .RequireAuthorization("Program");
    }
}
