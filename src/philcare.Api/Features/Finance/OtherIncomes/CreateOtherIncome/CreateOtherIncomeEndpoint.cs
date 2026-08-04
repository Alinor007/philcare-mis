using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Finance.OtherIncomes.CreateOtherIncome;

public sealed class CreateOtherIncomeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/other-income", async (CreateOtherIncomeRequest request, CreateOtherIncomeHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/other-income/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateOtherIncome")
        .WithTags("OtherIncome")
        .AddEndpointFilter<ValidationFilter<CreateOtherIncomeRequest>>()
        .RequireAuthorization("Finance");
    }
}
