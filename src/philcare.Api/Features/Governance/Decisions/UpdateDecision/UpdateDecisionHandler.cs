using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Decisions.UpdateDecision;

public sealed class UpdateDecisionHandler(AppDbContext db)
{
    public async Task<Result<UpdateDecisionResponse>> HandleAsync(int id, UpdateDecisionRequest request, CancellationToken cancellationToken)
    {
        var decision = await db.MeetingDecisions.Include(d => d.MeetingMinutes).FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (decision is null)
        {
            return Result.Failure<UpdateDecisionResponse>(Error.NotFound("Governance.DecisionNotFound", "Decision not found."));
        }

        if (decision.MeetingMinutes.PublicationStatus == MinutesStatus.Published)
        {
            return Result.Failure<UpdateDecisionResponse>(Error.Conflict("Governance.MinutesPublished", "Cannot edit a decision on published minutes."));
        }

        if (request.ResponsiblePersonId is not null)
        {
            var personExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.ResponsiblePersonId, cancellationToken);

            if (!personExists)
            {
                return Result.Failure<UpdateDecisionResponse>(Error.NotFound("Governance.ResponsiblePersonNotFound", "Responsible person not found."));
            }
        }

        decision.DecisionText = request.DecisionText;
        decision.ActionPoints = request.ActionPoints;
        decision.ResponsiblePersonId = request.ResponsiblePersonId;
        decision.DueDate = request.DueDate;
        decision.DecisionStatus = request.DecisionStatus;
        decision.Notes = request.Notes;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateDecisionResponse(decision.Id, decision.MeetingMinutesId, decision.DecisionText, decision.DecisionStatus));
    }
}
