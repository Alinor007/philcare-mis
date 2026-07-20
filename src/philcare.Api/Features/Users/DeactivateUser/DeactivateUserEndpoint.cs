using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Users.DeactivateUser;

public sealed class DeactivateUserEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/users/{id:int}", async (int id, ClaimsPrincipal claimsPrincipal, AppDbContext db, CancellationToken ct) =>
        {
            if (id == claimsPrincipal.GetUserId())
            {
                return Results.Problem(
                    title: "Users.CannotDeactivateSelf",
                    detail: "You cannot deactivate your own account.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

            if (user is null)
            {
                return Results.Problem(title: "Users.NotFound", detail: "User not found.", statusCode: StatusCodes.Status404NotFound);
            }

            user.IsActive = false;

            var tokens = await db.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync(ct);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeactivateUser")
        .WithTags("Users")
        .RequireAuthorization("Admin");
    }
}
