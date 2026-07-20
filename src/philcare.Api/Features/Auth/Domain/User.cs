using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Auth.Domain;

public class User : Entity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedOutUntil { get; set; }

    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
