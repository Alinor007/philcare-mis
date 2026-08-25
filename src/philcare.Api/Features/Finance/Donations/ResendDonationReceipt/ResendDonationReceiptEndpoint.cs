using philcare.Api.Common.Api;

namespace philcare.Api.Features.Finance.Donations.ResendDonationReceipt;

public sealed class ResendDonationReceiptEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/donations/{id:int}/resend-receipt", async (int id, ResendDonationReceiptHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("ResendDonationReceipt")
        .WithTags("Donations")
        .RequireAuthorization("Income");
    }
}
