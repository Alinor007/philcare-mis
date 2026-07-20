using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Auth.Services;

namespace philcare.Api.Features.Auth.RefreshToken;

public sealed class RefreshTokenHandler(AppDbContext db, ITokenService tokenService)
{
    public async Task<Result<RefreshTokenResponse>> HandleAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var existingToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (existingToken is null || !existingToken.IsActive || !existingToken.User.IsActive)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized("Auth.InvalidRefreshToken", "The refresh token is invalid or expired."));
        }

        existingToken.IsRevoked = true;

        var newRefreshTokenValue = tokenService.GenerateRefreshToken();
        var newRefreshTokenExpiry = tokenService.GetRefreshTokenExpiry();

        db.RefreshTokens.Add(new Domain.RefreshToken
        {
            Token = newRefreshTokenValue,
            ExpiresAt = newRefreshTokenExpiry,
            UserId = existingToken.UserId
        });

        var accessToken = tokenService.GenerateAccessToken(existingToken.User);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new RefreshTokenResponse(accessToken, newRefreshTokenValue, newRefreshTokenExpiry));
    }
}
