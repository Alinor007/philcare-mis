using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.Meetings.CreateMeeting;

public sealed class CreateMeetingEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/governance/meetings", async (CreateMeetingRequest request, CreateMeetingHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/governance/meetings/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateMeeting")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<CreateMeetingRequest>>()
        .RequireAuthorization("Admin");
    }
}
