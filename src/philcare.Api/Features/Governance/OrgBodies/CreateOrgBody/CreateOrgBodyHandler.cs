using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.OrgBodies.CreateOrgBody;

public sealed class CreateOrgBodyHandler(AppDbContext db)
{
    public async Task<Result<CreateOrgBodyResponse>> HandleAsync(CreateOrgBodyRequest request, CancellationToken cancellationToken)
    {
        var duplicateName = await db.OrgBodies.AnyAsync(b => b.Name == request.Name, cancellationToken);

        if (duplicateName)
        {
            return Result.Failure<CreateOrgBodyResponse>(Error.Conflict("Governance.DuplicateBodyName", "A governance body with this name already exists."));
        }

        if (request.ParentBodyId is not null)
        {
            var parentExists = await db.OrgBodies.AnyAsync(b => b.Id == request.ParentBodyId, cancellationToken);

            if (!parentExists)
            {
                return Result.Failure<CreateOrgBodyResponse>(Error.NotFound("Governance.ParentBodyNotFound", "Parent body not found."));
            }
        }

        var body = new OrgBody
        {
            Name = request.Name,
            BodyType = request.BodyType,
            ParentBodyId = request.ParentBodyId,
            QuorumRule = request.QuorumRule,
            DecisionThreshold = request.DecisionThreshold,
            MeetingFrequency = request.MeetingFrequency,
            PolicyBasis = request.PolicyBasis,
            Notes = request.Notes,
            IsActive = true
        };

        db.OrgBodies.Add(body);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateOrgBodyResponse(body.Id, body.Name, body.BodyType, body.ParentBodyId, body.IsActive));
    }
}
