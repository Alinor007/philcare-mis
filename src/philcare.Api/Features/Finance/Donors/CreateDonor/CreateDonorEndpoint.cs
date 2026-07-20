using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Finance.Donors.CreateDonor;

public sealed class CreateDonorEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/donors", async (CreateDonorRequest request, CreateDonorHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/donors/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateDonor")
        .WithTags("Donors")
        .AddEndpointFilter<ValidationFilter<CreateDonorRequest>>()
        .RequireAuthorization("Finance");
    }
}
