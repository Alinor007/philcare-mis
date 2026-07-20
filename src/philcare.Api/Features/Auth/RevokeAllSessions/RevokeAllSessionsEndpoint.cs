using System.Security.Claims;
using philcare.Api.Common.Api;

namespace philcare.Api.Features.Auth.RevokeAllSessions;

public sealed class RevokeAllSessionsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/revoke-all", async (
            ClaimsPrincipal claimsPrincipal,
            RevokeAllSessionsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(claimsPrincipal.GetUserId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .WithName("RevokeAllSessions")
        .WithTags("Auth")
        .RequireAuthorization();
    }
}
