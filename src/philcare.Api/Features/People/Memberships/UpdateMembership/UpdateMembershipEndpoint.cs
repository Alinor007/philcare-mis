using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.People.Memberships.UpdateMembership;

public sealed class UpdateMembershipEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/memberships/{id:int}", async (
            int id, UpdateMembershipRequest request, UpdateMembershipHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateMembership")
        .WithTags("Memberships")
        .AddEndpointFilter<ValidationFilter<UpdateMembershipRequest>>()
        .RequireAuthorization("Admin");
    }
}
