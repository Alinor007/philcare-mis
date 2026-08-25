using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.ProjectDonors.AddProjectDonor;

public sealed class AddProjectDonorEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/projects/{projectId:int}/donors", async (
            int projectId, AddProjectDonorRequest request, AddProjectDonorHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(projectId, request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/projects/{projectId}/donors", result.Value)
                : result.ToProblem();
        })
        .WithName("AddProjectDonor")
        .WithTags("Projects")
        .AddEndpointFilter<ValidationFilter<AddProjectDonorRequest>>()
        .RequireAuthorization("Program");
    }
}
