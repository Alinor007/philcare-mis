namespace philcare.Api.Features.Auth.ChangePassword;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
