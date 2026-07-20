using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Auth.Domain;
using philcare.Api.Features.Auth.Services;

namespace philcare.Api.Features.Auth.Register;

public sealed class RegisterHandler(AppDbContext db, IPasswordHasher passwordHasher)
{
    public async Task<Result<RegisterResponse>> HandleAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
        if (emailExists)
        {
            return Result.Failure<RegisterResponse>(
                Error.Conflict("Auth.EmailAlreadyExists", "A user with this email already exists."));
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new RegisterResponse(user.Id, user.Email, user.Role));
    }
}
