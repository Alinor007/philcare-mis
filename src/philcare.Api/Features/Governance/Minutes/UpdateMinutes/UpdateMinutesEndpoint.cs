using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.Minutes.UpdateMinutes;

public sealed class UpdateMinutesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/governance/meetings/{meetingId:int}/minutes", async (
            int meetingId, UpdateMinutesRequest request, UpdateMinutesHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(meetingId, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateMinutes")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<UpdateMinutesRequest>>()
        .RequireAuthorization("Admin");
    }
}
