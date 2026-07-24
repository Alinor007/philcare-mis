using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.Participants.UpdateParticipant;

public sealed class UpdateParticipantEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/participants/{id:int}", async (int id, UpdateParticipantRequest request, UpdateParticipantHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateParticipant")
        .WithTags("Participants")
        .AddEndpointFilter<ValidationFilter<UpdateParticipantRequest>>()
        .RequireAuthorization("Program");
    }
}
