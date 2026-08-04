using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Assignments.CreateAssignment;

public sealed class CreateAssignmentHandler(AppDbContext db)
{
    public async Task<Result<CreateAssignmentResponse>> HandleAsync(CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var person = await db.GovernancePeople.FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);

        if (person is null)
        {
            return Result.Failure<CreateAssignmentResponse>(Error.NotFound("Governance.PersonNotFound", "Person not found."));
        }

        if (!person.IsActive)
        {
            return Result.Failure<CreateAssignmentResponse>(Error.Validation("Governance.PersonInactive", "Cannot assign an inactive person."));
        }

        var body = await db.OrgBodies.FirstOrDefaultAsync(b => b.Id == request.OrgBodyId, cancellationToken);

        if (body is null)
        {
            return Result.Failure<CreateAssignmentResponse>(Error.NotFound("Governance.BodyNotFound", "Governance body not found."));
        }

        if (!body.IsActive)
        {
            return Result.Failure<CreateAssignmentResponse>(Error.Validation("Governance.BodyInactive", "Cannot assign to an inactive governance body."));
        }

        var role = await db.GovernanceRoles.FirstOrDefaultAsync(r => r.Id == request.GovernanceRoleId, cancellationToken);

        if (role is null)
        {
            return Result.Failure<CreateAssignmentResponse>(Error.NotFound("Governance.RoleNotFound", "Governance role not found."));
        }

        if (!role.IsActive)
        {
            return Result.Failure<CreateAssignmentResponse>(Error.Validation("Governance.RoleInactive", "Cannot assign an inactive governance role."));
        }

        if (request.IsPrimary)
        {
            var alreadyHasPrimary = await db.Assignments.AnyAsync(
                a => a.PersonId == request.PersonId && a.IsPrimaryCurrent == true, cancellationToken);

            if (alreadyHasPrimary)
            {
                return Result.Failure<CreateAssignmentResponse>(
                    Error.Conflict("Governance.DuplicatePrimaryAssignment", "This person already has a primary current assignment."));
            }
        }

        var assignment = new Assignment
        {
            PersonId = request.PersonId,
            OrgBodyId = request.OrgBodyId,
            GovernanceRoleId = request.GovernanceRoleId,
            PositionTitle = request.PositionTitle,
            StartDate = request.StartDate,
            IsPrimary = request.IsPrimary,
            VotingRights = request.VotingRights,
            IsTemporary = request.IsTemporary,
            Status = AssignmentStatus.Current,
            Notes = request.Notes,
            IsPrimaryCurrent = request.IsPrimary ? true : null
        };

        db.Assignments.Add(assignment);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (request.IsPrimary)
        {
            // The (PersonId, IsPrimaryCurrent) unique index caught a concurrent primary-assignment
            // creation that the AnyAsync pre-check above missed — lose cleanly with the same
            // conflict the pre-check would have returned, instead of surfacing a raw 500.
            return Result.Failure<CreateAssignmentResponse>(
                Error.Conflict("Governance.DuplicatePrimaryAssignment", "This person already has a primary current assignment."));
        }

        return Result.Success(new CreateAssignmentResponse(
            assignment.Id, assignment.PersonId, assignment.OrgBodyId, assignment.GovernanceRoleId, assignment.IsPrimary, assignment.Status.ToString()));
    }
}
