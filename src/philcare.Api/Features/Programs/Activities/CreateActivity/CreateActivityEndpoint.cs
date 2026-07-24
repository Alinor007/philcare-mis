using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.Activities.CreateActivity;

public sealed class CreateActivityEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/activities", async (CreateActivityRequest request, CreateActivityHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/activities/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateActivity")
        .WithTags("Activities")
        .AddEndpointFilter<ValidationFilter<CreateActivityRequest>>()
        .RequireAuthorization("Program");
    }
}
