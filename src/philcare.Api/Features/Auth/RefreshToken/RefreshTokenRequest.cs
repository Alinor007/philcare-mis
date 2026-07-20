namespace philcare.Api.Features.Auth.RefreshToken;

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record RefreshTokenResponse(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);
