using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Decisions.CreateDecision;

public sealed class CreateDecisionHandler(AppDbContext db)
{
    public async Task<Result<CreateDecisionResponse>> HandleAsync(int minutesId, CreateDecisionRequest request, CancellationToken cancellationToken)
    {
        var minutes = await db.MeetingMinutes.FirstOrDefaultAsync(mm => mm.Id == minutesId, cancellationToken);

        if (minutes is null)
        {
            return Result.Failure<CreateDecisionResponse>(Error.NotFound("Governance.MinutesNotFound", "Minutes not found."));
        }

        if (minutes.PublicationStatus == MinutesStatus.Published)
        {
            return Result.Failure<CreateDecisionResponse>(Error.Conflict("Governance.MinutesPublished", "Cannot add decisions to published minutes."));
        }

        if (request.ResponsiblePersonId is not null)
        {
            var personExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.ResponsiblePersonId, cancellationToken);

            if (!personExists)
            {
                return Result.Failure<CreateDecisionResponse>(Error.NotFound("Governance.ResponsiblePersonNotFound", "Responsible person not found."));
            }
        }

        var decision = new MeetingDecision
        {
            MeetingMinutesId = minutesId,
            DecisionText = request.DecisionText,
            ActionPoints = request.ActionPoints,
            ResponsiblePersonId = request.ResponsiblePersonId,
            DueDate = request.DueDate,
            DecisionStatus = request.DecisionStatus,
            Notes = request.Notes
        };

        db.MeetingDecisions.Add(decision);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateDecisionResponse(decision.Id, decision.MeetingMinutesId, decision.DecisionText, decision.DecisionStatus));
    }
}
