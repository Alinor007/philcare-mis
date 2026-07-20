using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Auth.Logout;

public sealed class LogoutHandler(AppDbContext db)
{
    public async Task<Result> HandleAsync(int userId, LogoutRequest request, CancellationToken cancellationToken)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId, cancellationToken);

        if (token is null)
        {
            return Result.Failure(Error.Unauthorized("Auth.InvalidRefreshToken", "The refresh token is invalid."));
        }

        token.IsRevoked = true;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
