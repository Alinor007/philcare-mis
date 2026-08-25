using FluentValidation;

namespace philcare.Api.Features.Programs.Projects.CreateProject;

public sealed class CreateProjectValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.ProgramId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FundType).MaximumLength(50);
        RuleFor(x => x.TotalBudget).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetBeneficiaries).GreaterThan(0).When(x => x.TargetBeneficiaries is not null);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.ProjectManager).MaximumLength(200);
        RuleFor(x => x.ApprovalLevel).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
