using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Zakat.UpdateZakatEligibility;

public sealed class UpdateZakatEligibilityEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/zakat-eligibilities/{id:int}", async (
            int id, UpdateZakatEligibilityRequest request, UpdateZakatEligibilityHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateZakatEligibility")
        .WithTags("ZakatEligibility")
        .AddEndpointFilter<ValidationFilter<UpdateZakatEligibilityRequest>>()
        .RequireAuthorization("Program");
    }
}
