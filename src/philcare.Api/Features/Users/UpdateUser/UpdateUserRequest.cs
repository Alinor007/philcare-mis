using philcare.Api.Features.Auth.Domain;

namespace philcare.Api.Features.Users.UpdateUser;

public sealed record UpdateUserRequest(UserRole Role, bool IsActive);

public sealed record UpdateUserResponse(int Id, string Email, UserRole Role, bool IsActive);
