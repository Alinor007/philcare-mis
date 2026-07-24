using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Zakat.Domain;

namespace philcare.Api.Features.Zakat.CreateZakatEligibility;

public sealed class CreateZakatEligibilityHandler(AppDbContext db)
{
    public async Task<Result<CreateZakatEligibilityResponse>> HandleAsync(CreateZakatEligibilityRequest request, CancellationToken cancellationToken)
    {
        var participant = await db.Participants.FirstOrDefaultAsync(p => p.Id == request.ParticipantId, cancellationToken);

        if (participant is null)
        {
            return Result.Failure<CreateZakatEligibilityResponse>(Error.NotFound("Zakat.ParticipantNotFound", "Participant not found."));
        }

        if (!participant.IsActive)
        {
            return Result.Failure<CreateZakatEligibilityResponse>(
                Error.Validation("Zakat.ParticipantInactive", "Cannot create a zakat eligibility case for an inactive participant."));
        }

        var eligibility = new ZakatEligibility
        {
            ParticipantId = request.ParticipantId,
            AsnafCategory = request.AsnafCategory,
            MonthlyIncomePhp = request.MonthlyIncomePhp,
            HouseholdSize = request.HouseholdSize,
            AssessmentDate = request.AssessmentDate,
            AssessedBy = request.AssessedBy,
            AssessmentNotes = request.AssessmentNotes,
            Notes = request.Notes,
            Status = ZakatEligibilityStatus.Draft
        };

        db.ZakatEligibilities.Add(eligibility);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateZakatEligibilityResponse(
            eligibility.Id, eligibility.ParticipantId, eligibility.AsnafCategory, eligibility.Status.ToString()));
    }
}
