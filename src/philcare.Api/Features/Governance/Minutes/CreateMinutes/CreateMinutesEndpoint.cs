using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.Minutes.CreateMinutes;

public sealed class CreateMinutesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/governance/meetings/{meetingId:int}/minutes", async (
            int meetingId, CreateMinutesRequest request, CreateMinutesHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(meetingId, request, ct);
            return result.IsSuccess ? Results.Created($"/api/governance/meetings/{meetingId}/minutes", result.Value) : result.ToProblem();
        })
        .WithName("CreateMinutes")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<CreateMinutesRequest>>()
        .RequireAuthorization("Admin");
    }
}
