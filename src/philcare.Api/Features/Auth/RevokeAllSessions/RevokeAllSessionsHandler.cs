using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Auth.RevokeAllSessions;

public sealed class RevokeAllSessionsHandler(AppDbContext db)
{
    public async Task<Result> HandleAsync(int userId, CancellationToken cancellationToken)
    {
        var tokens = await db.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
