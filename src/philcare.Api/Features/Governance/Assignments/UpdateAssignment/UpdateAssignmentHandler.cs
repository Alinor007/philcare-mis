using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Assignments.UpdateAssignment;

public sealed class UpdateAssignmentHandler(AppDbContext db)
{
    public async Task<Result<UpdateAssignmentResponse>> HandleAsync(int id, UpdateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (assignment is null)
        {
            return Result.Failure<UpdateAssignmentResponse>(Error.NotFound("Governance.AssignmentNotFound", "Assignment not found."));
        }

        var becomingPrimary = request.IsPrimary && !(assignment.IsPrimary && assignment.Status == AssignmentStatus.Current);

        if (becomingPrimary && assignment.Status == AssignmentStatus.Current)
        {
            var alreadyHasPrimary = await db.Assignments.AnyAsync(
                a => a.Id != id && a.PersonId == assignment.PersonId && a.IsPrimaryCurrent == true, cancellationToken);

            if (alreadyHasPrimary)
            {
                return Result.Failure<UpdateAssignmentResponse>(
                    Error.Conflict("Governance.DuplicatePrimaryAssignment", "This person already has a primary current assignment."));
            }
        }

        assignment.PositionTitle = request.PositionTitle;
        assignment.IsPrimary = request.IsPrimary;
        assignment.VotingRights = request.VotingRights;
        assignment.IsTemporary = request.IsTemporary;
        assignment.Notes = request.Notes;
        assignment.IsPrimaryCurrent = request.IsPrimary && assignment.Status == AssignmentStatus.Current ? true : null;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (becomingPrimary)
        {
            return Result.Failure<UpdateAssignmentResponse>(
                Error.Conflict("Governance.DuplicatePrimaryAssignment", "This person already has a primary current assignment."));
        }

        return Result.Success(new UpdateAssignmentResponse(
            assignment.Id, assignment.PersonId, assignment.OrgBodyId, assignment.GovernanceRoleId, assignment.IsPrimary, assignment.Status.ToString()));
    }
}
