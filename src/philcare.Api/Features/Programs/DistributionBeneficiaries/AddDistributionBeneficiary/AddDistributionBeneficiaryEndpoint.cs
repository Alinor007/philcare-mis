using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.DistributionBeneficiaries.AddDistributionBeneficiary;

public sealed class AddDistributionBeneficiaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/distributions/{distributionId:int}/beneficiaries", async (
            int distributionId, AddDistributionBeneficiaryRequest request, AddDistributionBeneficiaryHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(distributionId, request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/distributions/{distributionId}/beneficiaries", result.Value)
                : result.ToProblem();
        })
        .WithName("AddDistributionBeneficiary")
        .WithTags("DistributionBeneficiaries")
        .AddEndpointFilter<ValidationFilter<AddDistributionBeneficiaryRequest>>()
        .RequireAuthorization("Program");
    }
}
