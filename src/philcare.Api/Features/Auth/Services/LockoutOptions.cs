namespace philcare.Api.Features.Auth.Services;

public sealed record LockoutOptions
{
    public const string SectionName = "Lockout";

    public int MaxFailedAttempts { get; init; } = 5;
    public int LockoutMinutes { get; init; } = 15;
}
