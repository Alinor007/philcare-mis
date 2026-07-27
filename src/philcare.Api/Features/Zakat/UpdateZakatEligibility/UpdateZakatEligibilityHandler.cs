using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Zakat.Domain;

namespace philcare.Api.Features.Zakat.UpdateZakatEligibility;

public sealed class UpdateZakatEligibilityHandler(AppDbContext db)
{
    public async Task<Result<UpdateZakatEligibilityResponse>> HandleAsync(int id, UpdateZakatEligibilityRequest request, CancellationToken cancellationToken)
    {
        var eligibility = await db.ZakatEligibilities.FirstOrDefaultAsync(z => z.Id == id, cancellationToken);

        if (eligibility is null)
        {
            return Result.Failure<UpdateZakatEligibilityResponse>(Error.NotFound("Zakat.NotFound", "Zakat eligibility case not found."));
        }

        if (eligibility.Status != ZakatEligibilityStatus.Draft)
        {
            return Result.Failure<UpdateZakatEligibilityResponse>(
                Error.Conflict("Zakat.NotEditable", "Only a case in Draft status can be edited."));
        }

        eligibility.AsnafCategory = request.AsnafCategory;
        eligibility.MonthlyIncomePhp = request.MonthlyIncomePhp;
        eligibility.HouseholdSize = request.HouseholdSize;
        eligibility.AssessmentDate = request.AssessmentDate;
        eligibility.AssessedBy = request.AssessedBy;
        eligibility.AssessmentNotes = request.AssessmentNotes;
        eligibility.Notes = request.Notes;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateZakatEligibilityResponse(
            eligibility.Id, eligibility.ParticipantId, eligibility.AsnafCategory, eligibility.Status.ToString()));
    }
}
