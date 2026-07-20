using philcare.Api.Common.Api;

namespace philcare.Api.Features.Finance.Expenses.VoidExpense;

public sealed class VoidExpenseEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/expenses/{id:int}", async (int id, VoidExpenseHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .WithName("VoidExpense")
        .WithTags("Expenses")
        .RequireAuthorization("Admin");
    }
}
