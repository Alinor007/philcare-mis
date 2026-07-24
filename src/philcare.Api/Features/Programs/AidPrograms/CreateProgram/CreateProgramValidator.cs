using FluentValidation;

namespace philcare.Api.Features.Programs.AidPrograms.CreateProgram;

public sealed class CreateProgramValidator : AbstractValidator<CreateProgramRequest>
{
    public CreateProgramValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.OwnerDepartment).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
