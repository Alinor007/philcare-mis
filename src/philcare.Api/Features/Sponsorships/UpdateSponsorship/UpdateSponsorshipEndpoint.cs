using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Sponsorships.UpdateSponsorship;

public sealed class UpdateSponsorshipEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/sponsorships/{id:int}", async (int id, UpdateSponsorshipRequest request, UpdateSponsorshipHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateSponsorship")
        .WithTags("Sponsorships")
        .AddEndpointFilter<ValidationFilter<UpdateSponsorshipRequest>>()
        .RequireAuthorization("Program");
    }
}
