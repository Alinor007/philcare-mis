using FluentValidation;

namespace philcare.Api.Features.Programs.Projects.UpdateProject;

public sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FundCode).MaximumLength(20);
        RuleFor(x => x.TotalBudget).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetBeneficiaries).GreaterThan(0).When(x => x.TargetBeneficiaries is not null);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.ProjectManager).MaximumLength(200);
        RuleFor(x => x.ApprovalLevel).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
