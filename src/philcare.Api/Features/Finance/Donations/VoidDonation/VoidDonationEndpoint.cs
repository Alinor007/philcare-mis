using philcare.Api.Common.Api;

namespace philcare.Api.Features.Finance.Donations.VoidDonation;

public sealed class VoidDonationEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/donations/{id:int}", async (int id, VoidDonationHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .WithName("VoidDonation")
        .WithTags("Donations")
        .RequireAuthorization("Admin");
    }
}
