using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Finance.DonorEngagements.UpdateDonorEngagement;

public sealed class UpdateDonorEngagementEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/donor-engagements/{id:int}", async (int id, UpdateDonorEngagementRequest request, UpdateDonorEngagementHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateDonorEngagement")
        .WithTags("DonorEngagements")
        .AddEndpointFilter<ValidationFilter<UpdateDonorEngagementRequest>>()
        .RequireAuthorization("Income");
    }
}
