using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.Activities.ChangeActivityStatus;

public sealed class ChangeActivityStatusEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/activities/{id:int}/status", async (
            int id, ChangeActivityStatusRequest request, ChangeActivityStatusHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("ChangeActivityStatus")
        .WithTags("Activities")
        .AddEndpointFilter<ValidationFilter<ChangeActivityStatusRequest>>()
        .RequireAuthorization("Program");
    }
}
