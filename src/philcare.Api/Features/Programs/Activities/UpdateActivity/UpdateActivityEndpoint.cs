using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.Activities.UpdateActivity;

public sealed class UpdateActivityEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/activities/{id:int}", async (int id, UpdateActivityRequest request, UpdateActivityHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateActivity")
        .WithTags("Activities")
        .AddEndpointFilter<ValidationFilter<UpdateActivityRequest>>()
        .RequireAuthorization("Program");
    }
}
