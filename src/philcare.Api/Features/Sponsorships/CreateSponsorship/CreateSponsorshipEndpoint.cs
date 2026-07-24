using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Sponsorships.CreateSponsorship;

public sealed class CreateSponsorshipEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/sponsorships", async (CreateSponsorshipRequest request, CreateSponsorshipHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/sponsorships/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateSponsorship")
        .WithTags("Sponsorships")
        .AddEndpointFilter<ValidationFilter<CreateSponsorshipRequest>>()
        .RequireAuthorization("Program");
    }
}
