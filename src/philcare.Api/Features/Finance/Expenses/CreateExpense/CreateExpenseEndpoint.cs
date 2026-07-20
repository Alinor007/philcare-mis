using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Finance.Expenses.CreateExpense;

public sealed class CreateExpenseEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/expenses", async (CreateExpenseRequest request, CreateExpenseHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/expenses/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateExpense")
        .WithTags("Expenses")
        .AddEndpointFilter<ValidationFilter<CreateExpenseRequest>>()
        .RequireAuthorization("Finance");
    }
}
