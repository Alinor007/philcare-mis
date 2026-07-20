using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Auth.Services;

namespace philcare.Api.Features.Auth.Login;

public sealed class LoginHandler(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IOptions<LockoutOptions> lockoutOptions)
{
    private readonly LockoutOptions _lockoutOptions = lockoutOptions.Value;

    public async Task<Result<LoginResponse>> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure<LoginResponse>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        if (user.LockedOutUntil is { } lockedUntil && lockedUntil > DateTime.UtcNow)
        {
            return Result.Failure<LoginResponse>(
                Error.Locked("Auth.AccountLocked", $"Account is locked until {lockedUntil:O} due to too many failed login attempts."));
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= _lockoutOptions.MaxFailedAttempts)
            {
                user.FailedLoginAttempts = 0;
                user.LockedOutUntil = DateTime.UtcNow.AddMinutes(_lockoutOptions.LockoutMinutes);
                await db.SaveChangesAsync(cancellationToken);

                return Result.Failure<LoginResponse>(
                    Error.Locked("Auth.AccountLocked", $"Account has been locked for {_lockoutOptions.LockoutMinutes} minutes due to too many failed login attempts."));
            }

            await db.SaveChangesAsync(cancellationToken);

            return Result.Failure<LoginResponse>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        user.FailedLoginAttempts = 0;
        user.LockedOutUntil = null;

        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshTokenValue = tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = tokenService.GetRefreshTokenExpiry();

        db.RefreshTokens.Add(new Domain.RefreshToken
        {
            Token = refreshTokenValue,
            ExpiresAt = refreshTokenExpiry,
            UserId = user.Id
        });

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResponse(accessToken, refreshTokenValue, refreshTokenExpiry));
    }
}
