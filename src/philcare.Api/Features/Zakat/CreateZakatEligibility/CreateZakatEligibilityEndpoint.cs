using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Zakat.CreateZakatEligibility;

public sealed class CreateZakatEligibilityEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/zakat-eligibilities", async (
            CreateZakatEligibilityRequest request, CreateZakatEligibilityHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/zakat-eligibilities/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateZakatEligibility")
        .WithTags("ZakatEligibility")
        .AddEndpointFilter<ValidationFilter<CreateZakatEligibilityRequest>>()
        .RequireAuthorization("Casework");
    }
}
