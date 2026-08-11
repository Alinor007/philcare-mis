using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Governance.People.UpdatePerson;

public sealed class UpdatePersonEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/governance/people/{id:int}", async (int id, UpdatePersonRequest request, UpdatePersonHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdatePerson")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<UpdatePersonRequest>>()
        .RequireAuthorization("Admin");
    }
}
