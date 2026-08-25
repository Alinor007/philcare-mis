using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Zakat.Domain;

namespace philcare.Api.Features.Zakat.SubmitZakatEligibility;

public sealed class SubmitZakatEligibilityHandler(AppDbContext db)
{
    public async Task<Result<SubmitZakatEligibilityResponse>> HandleAsync(int id, CancellationToken cancellationToken)
    {
        var eligibility = await db.ZakatEligibilities.FirstOrDefaultAsync(z => z.Id == id, cancellationToken);

        if (eligibility is null)
        {
            return Result.Failure<SubmitZakatEligibilityResponse>(Error.NotFound("Zakat.NotFound", "Zakat eligibility case not found."));
        }

        if (eligibility.Status != ZakatEligibilityStatus.Draft)
        {
            return Result.Failure<SubmitZakatEligibilityResponse>(
                Error.Conflict("Zakat.NotSubmittable", "Only a case in Draft status can be submitted."));
        }

        var alreadyApproved = await db.ZakatEligibilities.AnyAsync(
            z => z.Id != id && z.BeneficiaryId == eligibility.BeneficiaryId && z.Status == ZakatEligibilityStatus.Approved
                && (z.ValidUntil == null || z.ValidUntil >= DateTime.UtcNow.Date),
            cancellationToken);

        if (alreadyApproved)
        {
            return Result.Failure<SubmitZakatEligibilityResponse>(
                Error.Conflict("Zakat.AlreadyApproved", "This beneficiary already has an approved, unexpired zakat eligibility case."));
        }

        eligibility.Status = ZakatEligibilityStatus.Submitted;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new SubmitZakatEligibilityResponse(eligibility.Id, eligibility.Status.ToString()));
    }
}
