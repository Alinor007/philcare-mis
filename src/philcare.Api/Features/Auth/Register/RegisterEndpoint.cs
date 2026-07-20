using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;

namespace philcare.Api.Features.Auth.Register;

public sealed class RegisterEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (RegisterRequest request, RegisterHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.IsSuccess ? Results.Created($"/api/auth/{result.Value.Id}", result.Value) : result.ToProblem();
        })
        .WithName("Register")
        .WithTags("Auth")
        .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
        .RequireAuthorization("Admin");
    }
}
