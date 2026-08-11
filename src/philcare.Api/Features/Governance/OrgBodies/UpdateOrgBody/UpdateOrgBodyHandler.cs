using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.OrgBodies.UpdateOrgBody;

public sealed class UpdateOrgBodyHandler(AppDbContext db)
{
    public async Task<Result<UpdateOrgBodyResponse>> HandleAsync(int id, UpdateOrgBodyRequest request, CancellationToken cancellationToken)
    {
        var body = await db.OrgBodies.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (body is null)
        {
            return Result.Failure<UpdateOrgBodyResponse>(Error.NotFound("Governance.BodyNotFound", "Governance body not found."));
        }

        var duplicateName = await db.OrgBodies.AnyAsync(b => b.Id != id && b.Name == request.Name, cancellationToken);

        if (duplicateName)
        {
            return Result.Failure<UpdateOrgBodyResponse>(Error.Conflict("Governance.DuplicateBodyName", "A governance body with this name already exists."));
        }

        if (request.ParentBodyId is not null)
        {
            if (request.ParentBodyId == id)
            {
                return Result.Failure<UpdateOrgBodyResponse>(
                    Error.Validation("Governance.CircularBodyHierarchy", "A governance body cannot be its own parent."));
            }

            var parentExists = await db.OrgBodies.AnyAsync(b => b.Id == request.ParentBodyId, cancellationToken);

            if (!parentExists)
            {
                return Result.Failure<UpdateOrgBodyResponse>(Error.NotFound("Governance.ParentBodyNotFound", "Parent body not found."));
            }

            // Walk the proposed parent's ancestor chain — if it reaches this body, the change
            // would create a cycle. Bounded by total body count to avoid an infinite loop should
            // the existing data somehow already contain one.
            var totalBodies = await db.OrgBodies.CountAsync(cancellationToken);
            var currentId = request.ParentBodyId;
            var hops = 0;

            while (currentId is not null && hops <= totalBodies)
            {
                if (currentId == id)
                {
                    return Result.Failure<UpdateOrgBodyResponse>(
                        Error.Validation("Governance.CircularBodyHierarchy", "This parent assignment would create a circular governance hierarchy."));
                }

                currentId = await db.OrgBodies.Where(b => b.Id == currentId).Select(b => b.ParentBodyId).FirstOrDefaultAsync(cancellationToken);
                hops++;
            }
        }

        body.Name = request.Name;
        body.BodyType = request.BodyType;
        body.ParentBodyId = request.ParentBodyId;
        body.QuorumRule = request.QuorumRule;
        body.DecisionThreshold = request.DecisionThreshold;
        body.MeetingFrequency = request.MeetingFrequency;
        body.PolicyBasis = request.PolicyBasis;
        body.Notes = request.Notes;
        body.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateOrgBodyResponse(body.Id, body.Name, body.BodyType, body.ParentBodyId, body.IsActive));
    }
}
