using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.Participants.CreateParticipant;

public sealed class CreateParticipantEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/participants", async (CreateParticipantRequest request, CreateParticipantHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/participants/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateParticipant")
        .WithTags("Participants")
        .AddEndpointFilter<ValidationFilter<CreateParticipantRequest>>()
        .RequireAuthorization("Program");
    }
}
