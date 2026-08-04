using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Finance.DonorEngagements.CreateDonorEngagement;

public sealed class CreateDonorEngagementEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/donor-engagements", async (CreateDonorEngagementRequest request, CreateDonorEngagementHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/donor-engagements/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateDonorEngagement")
        .WithTags("DonorEngagements")
        .AddEndpointFilter<ValidationFilter<CreateDonorEngagementRequest>>()
        .RequireAuthorization("Finance");
    }
}
