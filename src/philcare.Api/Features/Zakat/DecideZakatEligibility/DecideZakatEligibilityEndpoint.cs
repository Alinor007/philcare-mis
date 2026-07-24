using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Zakat.DecideZakatEligibility;

public sealed class DecideZakatEligibilityEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/zakat-eligibilities/{id:int}/decision", async (
            int id, DecideZakatEligibilityRequest request, DecideZakatEligibilityHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("DecideZakatEligibility")
        .WithTags("ZakatEligibility")
        .AddEndpointFilter<ValidationFilter<DecideZakatEligibilityRequest>>()
        .RequireAuthorization("Admin");
    }
}
