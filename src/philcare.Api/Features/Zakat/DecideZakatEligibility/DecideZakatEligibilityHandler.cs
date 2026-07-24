using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Zakat.Domain;

namespace philcare.Api.Features.Zakat.DecideZakatEligibility;

public sealed class DecideZakatEligibilityHandler(AppDbContext db)
{
    public async Task<Result<DecideZakatEligibilityResponse>> HandleAsync(int id, DecideZakatEligibilityRequest request, CancellationToken cancellationToken)
    {
        var eligibility = await db.ZakatEligibilities.FirstOrDefaultAsync(z => z.Id == id, cancellationToken);

        if (eligibility is null)
        {
            return Result.Failure<DecideZakatEligibilityResponse>(Error.NotFound("Zakat.NotFound", "Zakat eligibility case not found."));
        }

        if (eligibility.Status != ZakatEligibilityStatus.Submitted)
        {
            return Result.Failure<DecideZakatEligibilityResponse>(
                Error.Conflict("Zakat.NotDecidable", "Only a case in Submitted status can be decided."));
        }

        if (request.Approve)
        {
            var alreadyApproved = await db.ZakatEligibilities.AnyAsync(
                z => z.Id != id && z.ParticipantId == eligibility.ParticipantId && z.Status == ZakatEligibilityStatus.Approved
                    && (z.ValidUntil == null || z.ValidUntil >= DateTime.UtcNow.Date),
                cancellationToken);

            if (alreadyApproved)
            {
                return Result.Failure<DecideZakatEligibilityResponse>(
                    Error.Conflict("Zakat.AlreadyApproved", "This participant already has an approved, unexpired zakat eligibility case."));
            }

            eligibility.Status = ZakatEligibilityStatus.Approved;
            eligibility.DecisionDate = DateTime.UtcNow.Date;
            eligibility.DecidedBy = request.DecidedBy;
            eligibility.ValidUntil = request.ValidUntil ?? DateTime.UtcNow.Date.AddMonths(12);
        }
        else
        {
            eligibility.Status = ZakatEligibilityStatus.Rejected;
            eligibility.DecisionDate = DateTime.UtcNow.Date;
            eligibility.DecidedBy = request.DecidedBy;
            eligibility.RejectionReason = request.RejectionReason;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new DecideZakatEligibilityResponse(
            eligibility.Id, eligibility.Status.ToString(), eligibility.ValidUntil, eligibility.RejectionReason));
    }
}
