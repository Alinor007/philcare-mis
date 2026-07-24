using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.Distributions.CreateDistribution;

public sealed class CreateDistributionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/distributions", async (CreateDistributionRequest request, CreateDistributionHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/distributions/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateDistribution")
        .WithTags("Distributions")
        .AddEndpointFilter<ValidationFilter<CreateDistributionRequest>>()
        .RequireAuthorization("Program");
    }
}
