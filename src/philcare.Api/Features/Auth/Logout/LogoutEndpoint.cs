using System.Security.Claims;
using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Auth.Logout;

public sealed class LogoutEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/logout", async (
            LogoutRequest request,
            ClaimsPrincipal claimsPrincipal,
            LogoutHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(claimsPrincipal.GetUserId(), request, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .WithName("Logout")
        .WithTags("Auth")
        .AddEndpointFilter<ValidationFilter<LogoutRequest>>()
        .RequireAuthorization();
    }
}
