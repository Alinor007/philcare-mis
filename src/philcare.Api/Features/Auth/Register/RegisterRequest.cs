using philcare.Api.Features.Auth.Domain;

namespace philcare.Api.Features.Auth.Register;

public sealed record RegisterRequest(string Email, string Password, UserRole Role);

public sealed record RegisterResponse(int Id, string Email, UserRole Role);
