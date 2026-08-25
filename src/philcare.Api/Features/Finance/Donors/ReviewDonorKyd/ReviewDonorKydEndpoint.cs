using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Finance.Donors.ReviewDonorKyd;

public sealed class ReviewDonorKydEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/donors/{id:int}/kyd-status", async (
            int id, ReviewDonorKydRequest request, ReviewDonorKydHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("ReviewDonorKyd")
        .WithTags("Donors")
        .AddEndpointFilter<ValidationFilter<ReviewDonorKydRequest>>()
        .RequireAuthorization("Income");
    }
}
