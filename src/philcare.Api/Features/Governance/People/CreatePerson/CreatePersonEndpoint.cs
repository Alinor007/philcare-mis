using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.People.CreatePerson;

public sealed class CreatePersonEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/governance/people", async (CreatePersonRequest request, CreatePersonHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/governance/people/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreatePerson")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<CreatePersonRequest>>()
        .RequireAuthorization("Admin");
    }
}
