using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.MeetingParticipants.AddMeetingParticipant;

public sealed class AddMeetingParticipantEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/governance/meetings/{meetingId:int}/participants", async (
            int meetingId, AddMeetingParticipantRequest request, AddMeetingParticipantHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(meetingId, request, ct);
            return result.IsSuccess ? Results.Created($"/api/governance/meetings/{meetingId}/participants", result.Value) : result.ToProblem();
        })
        .WithName("AddMeetingParticipant")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<AddMeetingParticipantRequest>>()
        .RequireAuthorization("Admin");
    }
}
