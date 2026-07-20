using FluentValidation;

namespace philcare.Api.Features.Users.UpdateUser;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}
