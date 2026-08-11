using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.ActivityParticipants.UpdateActivityParticipant;

public sealed class UpdateActivityParticipantEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/activities/{activityId:int}/participants/{staffMemberId:int}", async (
            int activityId, int staffMemberId, UpdateActivityParticipantRequest request,
            UpdateActivityParticipantHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(activityId, staffMemberId, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateActivityParticipant")
        .WithTags("ActivityParticipants")
        .AddEndpointFilter<ValidationFilter<UpdateActivityParticipantRequest>>()
        .RequireAuthorization("Program");
    }
}
