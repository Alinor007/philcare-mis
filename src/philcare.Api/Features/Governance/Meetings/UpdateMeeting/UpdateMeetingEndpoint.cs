using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.Meetings.UpdateMeeting;

public sealed class UpdateMeetingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/governance/meetings/{id:int}", async (int id, UpdateMeetingRequest request, UpdateMeetingHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateMeeting")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<UpdateMeetingRequest>>()
        .RequireAuthorization("Admin");
    }
}
