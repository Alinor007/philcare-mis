using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Users.UpdateUser;

public sealed class UpdateUserHandler(AppDbContext db)
{
    public async Task<Result<UpdateUserResponse>> HandleAsync(int id, int currentUserId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UpdateUserResponse>(Error.NotFound("Users.NotFound", "User not found."));
        }

        if (user.Id == currentUserId && (!request.IsActive || request.Role != user.Role))
        {
            return Result.Failure<UpdateUserResponse>(
                Error.Validation("Users.CannotModifySelf", "You cannot change your own role or active status."));
        }

        user.Role = request.Role;
        user.IsActive = request.IsActive;

        if (!request.IsActive)
        {
            var tokens = await db.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateUserResponse(user.Id, user.Email, user.Role, user.IsActive));
    }
}
