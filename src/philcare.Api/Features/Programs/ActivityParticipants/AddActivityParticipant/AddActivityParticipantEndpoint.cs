using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.ActivityParticipants.AddActivityParticipant;

public sealed class AddActivityParticipantEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/activities/{activityId:int}/participants", async (
            int activityId, AddActivityParticipantRequest request, AddActivityParticipantHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(activityId, request, ct);
            return result.IsSuccess ? Results.Created($"/api/activities/{activityId}/participants", result.Value) : result.ToProblem();
        })
        .WithName("AddActivityParticipant")
        .WithTags("ActivityParticipants")
        .AddEndpointFilter<ValidationFilter<AddActivityParticipantRequest>>()
        .RequireAuthorization("Program");
    }
}
