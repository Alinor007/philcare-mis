using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.DistributionBeneficiaries.UpdateDistributionBeneficiary;

public sealed class UpdateDistributionBeneficiaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/distributions/{distributionId:int}/beneficiaries/{beneficiaryId:int}", async (
            int distributionId, int beneficiaryId, UpdateDistributionBeneficiaryRequest request,
            UpdateDistributionBeneficiaryHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(distributionId, beneficiaryId, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateDistributionBeneficiary")
        .WithTags("DistributionBeneficiaries")
        .AddEndpointFilter<ValidationFilter<UpdateDistributionBeneficiaryRequest>>()
        .RequireAuthorization("Program");
    }
}
