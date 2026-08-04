using philcare.Api.Common.Api;

namespace philcare.Api.Features.Finance.OtherIncomes.VoidOtherIncome;

public sealed class VoidOtherIncomeEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/other-income/{id:int}", async (int id, VoidOtherIncomeHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .WithName("VoidOtherIncome")
        .WithTags("OtherIncome")
        .RequireAuthorization("Admin");
    }
}
