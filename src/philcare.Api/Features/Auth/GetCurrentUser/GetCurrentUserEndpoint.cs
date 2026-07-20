using System.Security.Claims;
using philcare.Api.Common.Api;

namespace philcare.Api.Features.Auth.GetCurrentUser;

public sealed record CurrentUserResponse(int Id, string Email, string Role);

public sealed class GetCurrentUserEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", (ClaimsPrincipal user) =>
        {
            var id = user.GetUserId();
            var email = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity!.Name!;
            var role = user.FindFirstValue(ClaimTypes.Role)!;

            return Results.Ok(new CurrentUserResponse(id, email, role));
        })
        .WithName("GetCurrentUser")
        .WithTags("Auth")
        .RequireAuthorization();
    }
}
