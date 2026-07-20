using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Auth.Domain;

namespace philcare.Api.Features.Users.GetUsers;

public sealed record UserResponse(int Id, string Email, UserRole Role, bool IsActive, DateTime CreatedAt);

public sealed class GetUsersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/users", async (AppDbContext db, CancellationToken ct) =>
        {
            var users = await db.Users
                .OrderBy(u => u.Email)
                .Select(u => new UserResponse(u.Id, u.Email, u.Role, u.IsActive, u.CreatedAt))
                .ToListAsync(ct);

            return Results.Ok(users);
        })
        .WithName("GetUsers")
        .WithTags("Users")
        .RequireAuthorization("Admin");
    }
}
