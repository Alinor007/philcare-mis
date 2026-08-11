using FluentValidation;

namespace philcare.Api.Features.Governance.Roles.CreateGovernanceRole;

public sealed class CreateGovernanceRoleValidator : AbstractValidator<CreateGovernanceRoleRequest>
{
    public CreateGovernanceRoleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RoleCategory).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DefaultBodyId).GreaterThan(0).When(x => x.DefaultBodyId is not null);
        RuleFor(x => x.DefaultVotingRights).MaximumLength(100);
        RuleFor(x => x.CountsForQuorum).MaximumLength(100);
        RuleFor(x => x.Delegable).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
