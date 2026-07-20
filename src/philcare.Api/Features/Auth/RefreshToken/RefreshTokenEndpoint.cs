using philcare.Api.Common.Api;

namespace philcare.Api.Features.Auth.RefreshToken;

public sealed class RefreshTokenEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refresh", async (RefreshTokenRequest request, RefreshTokenHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        })
        .WithName("RefreshToken")
        .WithTags("Auth")
        .AllowAnonymous();
    }
}
