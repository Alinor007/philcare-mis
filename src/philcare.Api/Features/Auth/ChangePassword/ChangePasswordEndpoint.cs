using System.Security.Claims;
using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Auth.ChangePassword;

public sealed class ChangePasswordEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/change-password", async (
            ChangePasswordRequest request,
            ClaimsPrincipal claimsPrincipal,
            ChangePasswordHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(claimsPrincipal.GetUserId(), request, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        })
        .WithName("ChangePassword")
        .WithTags("Auth")
        .AddEndpointFilter<ValidationFilter<ChangePasswordRequest>>()
        .RequireAuthorization();
    }
}
