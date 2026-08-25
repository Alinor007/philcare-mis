using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Programs.Beneficiaries.CreateBeneficiary;

public sealed class CreateBeneficiaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/beneficiaries", async (CreateBeneficiaryRequest request, CreateBeneficiaryHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/beneficiaries/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateBeneficiary")
        .WithTags("Beneficiaries")
        .AddEndpointFilter<ValidationFilter<CreateBeneficiaryRequest>>()
        .RequireAuthorization("Program");
    }
}
