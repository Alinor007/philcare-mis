using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Zakat.Domain;

namespace philcare.Api.Features.Zakat.CreateZakatEligibility;

public sealed class CreateZakatEligibilityHandler(AppDbContext db)
{
    public async Task<Result<CreateZakatEligibilityResponse>> HandleAsync(CreateZakatEligibilityRequest request, CancellationToken cancellationToken)
    {
        var beneficiary = await db.Beneficiaries.FirstOrDefaultAsync(p => p.Id == request.BeneficiaryId, cancellationToken);

        if (beneficiary is null)
        {
            return Result.Failure<CreateZakatEligibilityResponse>(Error.NotFound("Zakat.BeneficiaryNotFound", "Beneficiary not found."));
        }

        if (!beneficiary.IsActive)
        {
            return Result.Failure<CreateZakatEligibilityResponse>(
                Error.Validation("Zakat.BeneficiaryInactive", "Cannot create a zakat eligibility case for an inactive beneficiary."));
        }

        var eligibility = new ZakatEligibility
        {
            BeneficiaryId = request.BeneficiaryId,
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
            eligibility.Id, eligibility.BeneficiaryId, eligibility.AsnafCategory, eligibility.Status.ToString()));
    }
}
