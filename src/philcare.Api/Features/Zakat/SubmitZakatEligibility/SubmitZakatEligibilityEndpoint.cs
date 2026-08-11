using philcare.Api.Common.Api;

namespace philcare.Api.Features.Zakat.SubmitZakatEligibility;

public sealed class SubmitZakatEligibilityEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/zakat-eligibilities/{id:int}/submit", async (int id, SubmitZakatEligibilityHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("SubmitZakatEligibility")
        .WithTags("ZakatEligibility")
        .RequireAuthorization("Casework");
    }
}
