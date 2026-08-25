using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.People.Memberships.CreateMembership;

public sealed class CreateMembershipEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/memberships", async (CreateMembershipRequest request, CreateMembershipHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/memberships/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("CreateMembership")
        .WithTags("Memberships")
        .AddEndpointFilter<ValidationFilter<CreateMembershipRequest>>()
        .RequireAuthorization("Admin");
    }
}
