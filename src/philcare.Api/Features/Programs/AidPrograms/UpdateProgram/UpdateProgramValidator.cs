using FluentValidation;

namespace philcare.Api.Features.Programs.AidPrograms.UpdateProgram;

public sealed class UpdateProgramValidator : AbstractValidator<UpdateProgramRequest>
{
    public UpdateProgramValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.OwnerDepartment).MaximumLength(200);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
