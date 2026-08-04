using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.Roles.UpdateGovernanceRole;

public sealed class UpdateGovernanceRoleHandler(AppDbContext db)
{
    public async Task<Result<UpdateGovernanceRoleResponse>> HandleAsync(int id, UpdateGovernanceRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await db.GovernanceRoles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (role is null)
        {
            return Result.Failure<UpdateGovernanceRoleResponse>(Error.NotFound("Governance.RoleNotFound", "Governance role not found."));
        }

        var duplicateName = await db.GovernanceRoles.AnyAsync(r => r.Id != id && r.Name == request.Name, cancellationToken);

        if (duplicateName)
        {
            return Result.Failure<UpdateGovernanceRoleResponse>(Error.Conflict("Governance.DuplicateRoleName", "A governance role with this name already exists."));
        }

        if (request.DefaultBodyId is not null)
        {
            var bodyExists = await db.OrgBodies.AnyAsync(b => b.Id == request.DefaultBodyId, cancellationToken);

            if (!bodyExists)
            {
                return Result.Failure<UpdateGovernanceRoleResponse>(Error.NotFound("Governance.BodyNotFound", "Governance body not found."));
            }
        }

        role.Name = request.Name;
        role.RoleCategory = request.RoleCategory;
        role.DefaultBodyId = request.DefaultBodyId;
        role.DefaultVotingRights = request.DefaultVotingRights;
        role.CountsForQuorum = request.CountsForQuorum;
        role.Delegable = request.Delegable;
        role.Notes = request.Notes;
        role.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateGovernanceRoleResponse(role.Id, role.Name, role.RoleCategory, role.IsActive));
    }
}
