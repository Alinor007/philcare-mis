using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Roles.CreateGovernanceRole;

public sealed class CreateGovernanceRoleHandler(AppDbContext db)
{
    public async Task<Result<CreateGovernanceRoleResponse>> HandleAsync(CreateGovernanceRoleRequest request, CancellationToken cancellationToken)
    {
        var duplicateName = await db.GovernanceRoles.AnyAsync(r => r.Name == request.Name, cancellationToken);

        if (duplicateName)
        {
            return Result.Failure<CreateGovernanceRoleResponse>(Error.Conflict("Governance.DuplicateRoleName", "A governance role with this name already exists."));
        }

        if (request.DefaultBodyId is not null)
        {
            var bodyExists = await db.OrgBodies.AnyAsync(b => b.Id == request.DefaultBodyId, cancellationToken);

            if (!bodyExists)
            {
                return Result.Failure<CreateGovernanceRoleResponse>(Error.NotFound("Governance.BodyNotFound", "Governance body not found."));
            }
        }

        var role = new GovernanceRole
        {
            Name = request.Name,
            RoleCategory = request.RoleCategory,
            DefaultBodyId = request.DefaultBodyId,
            DefaultVotingRights = request.DefaultVotingRights,
            CountsForQuorum = request.CountsForQuorum,
            Delegable = request.Delegable,
            Notes = request.Notes,
            IsActive = true
        };

        db.GovernanceRoles.Add(role);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateGovernanceRoleResponse(role.Id, role.Name, role.RoleCategory, role.IsActive));
    }
}
