using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Sponsorships.ChangeSponsorshipStatus;

public sealed class ChangeSponsorshipStatusEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/sponsorships/{id:int}/status", async (
            int id, ChangeSponsorshipStatusRequest request, ChangeSponsorshipStatusHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("ChangeSponsorshipStatus")
        .WithTags("Sponsorships")
        .AddEndpointFilter<ValidationFilter<ChangeSponsorshipStatusRequest>>()
        .RequireAuthorization("Program");
    }
}
