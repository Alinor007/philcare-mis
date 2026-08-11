using philcare.Api.Common.Api;

namespace philcare.Api.Features.Programs.Distributions.VoidDistribution;

public sealed class VoidDistributionEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/distributions/{id:int}", async (int id, VoidDistributionHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .WithName("VoidDistribution")
        .WithTags("Distributions")
        .RequireAuthorization("Admin");
    }
}
