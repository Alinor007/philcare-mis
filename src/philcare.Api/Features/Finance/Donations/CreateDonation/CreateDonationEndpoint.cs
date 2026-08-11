using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Finance.Donations.CreateDonation;

public sealed class CreateDonationEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/donations", async (CreateDonationRequest request, CreateDonationHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/donations/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateDonation")
        .WithTags("Donations")
        .AddEndpointFilter<ValidationFilter<CreateDonationRequest>>()
        .RequireAuthorization("Income");
    }
}
