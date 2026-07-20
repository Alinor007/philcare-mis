using System.Security.Claims;
using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Users.UpdateUser;

public sealed class UpdateUserEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/users/{id:int}", async (
            int id,
            UpdateUserRequest request,
            ClaimsPrincipal claimsPrincipal,
            UpdateUserHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, claimsPrincipal.GetUserId(), request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("UpdateUser")
        .WithTags("Users")
        .AddEndpointFilter<ValidationFilter<UpdateUserRequest>>()
        .RequireAuthorization("Admin");
    }
}
