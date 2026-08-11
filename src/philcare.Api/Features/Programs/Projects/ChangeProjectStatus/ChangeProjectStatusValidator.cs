using FluentValidation;

namespace philcare.Api.Features.Programs.Projects.ChangeProjectStatus;

public sealed class ChangeProjectStatusValidator : AbstractValidator<ChangeProjectStatusRequest>
{
    public ChangeProjectStatusValidator()
    {
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
    }
}
