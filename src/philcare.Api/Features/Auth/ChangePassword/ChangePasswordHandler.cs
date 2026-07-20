using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Auth.Services;

namespace philcare.Api.Features.Auth.ChangePassword;

public sealed class ChangePasswordHandler(AppDbContext db, IPasswordHasher passwordHasher)
{
    public async Task<Result> HandleAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(Error.NotFound("Auth.UserNotFound", "User not found."));
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(Error.Validation("Auth.InvalidCurrentPassword", "The current password is incorrect."));
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
