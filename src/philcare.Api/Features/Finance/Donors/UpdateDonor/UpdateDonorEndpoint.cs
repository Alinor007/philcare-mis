using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Finance.Donors.UpdateDonor;

public sealed class UpdateDonorEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/donors/{id:int}", async (int id, UpdateDonorRequest request, UpdateDonorHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateDonor")
        .WithTags("Donors")
        .AddEndpointFilter<ValidationFilter<UpdateDonorRequest>>()
        .RequireAuthorization("Income");
    }
}
