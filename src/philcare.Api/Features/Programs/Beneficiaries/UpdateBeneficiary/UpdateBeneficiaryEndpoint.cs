using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.Beneficiaries.UpdateBeneficiary;

public sealed class UpdateBeneficiaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/beneficiaries/{id:int}", async (int id, UpdateBeneficiaryRequest request, UpdateBeneficiaryHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateBeneficiary")
        .WithTags("Beneficiaries")
        .AddEndpointFilter<ValidationFilter<UpdateBeneficiaryRequest>>()
        .RequireAuthorization("Program");
    }
}
