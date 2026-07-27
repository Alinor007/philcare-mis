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
            var today = DateTime.UtcNow.Date;

            var alreadyApproved = await db.ZakatEligibilities.AnyAsync(
                z => z.Id != id && z.ParticipantId == eligibility.ParticipantId && z.Status == ZakatEligibilityStatus.Approved
                    && (z.ValidUntil == null || z.ValidUntil >= today),
                cancellationToken);

            if (alreadyApproved)
            {
                return Result.Failure<DecideZakatEligibilityResponse>(
                    Error.Conflict("Zakat.AlreadyApproved", "This participant already has an approved, unexpired zakat eligibility case."));
            }

            // Clear the live-approval flag on any of this participant's Approved cases that have since
            // expired, so a fresh approval doesn't collide with the (ParticipantId, IsLiveApproval)
            // unique index — that index is what actually closes the concurrent-approval race; the
            // AnyAsync check above is just a friendly pre-flight for the common case.
            var expiredLiveApprovals = await db.ZakatEligibilities
                .Where(z => z.ParticipantId == eligibility.ParticipantId && z.IsLiveApproval == true
                    && z.ValidUntil != null && z.ValidUntil < today)
                .ToListAsync(cancellationToken);

            foreach (var expired in expiredLiveApprovals)
            {
                expired.IsLiveApproval = null;
            }

            eligibility.Status = ZakatEligibilityStatus.Approved;
            eligibility.DecisionDate = today;
            eligibility.DecidedBy = request.DecidedBy;
            eligibility.ValidUntil = request.ValidUntil ?? today.AddMonths(12);
            eligibility.IsLiveApproval = true;
        }
        else
        {
            eligibility.Status = ZakatEligibilityStatus.Rejected;
            eligibility.DecisionDate = DateTime.UtcNow.Date;
            eligibility.DecidedBy = request.DecidedBy;
            eligibility.RejectionReason = request.RejectionReason;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (request.Approve)
        {
            // The (ParticipantId, IsLiveApproval) unique index caught a concurrent approval that the
            // AnyAsync pre-check above missed — lose cleanly with the same conflict the pre-check would
            // have returned, instead of surfacing a raw 500.
            return Result.Failure<DecideZakatEligibilityResponse>(
                Error.Conflict("Zakat.AlreadyApproved", "This participant already has an approved, unexpired zakat eligibility case."));
        }

        return Result.Success(new DecideZakatEligibilityResponse(
            eligibility.Id, eligibility.Status.ToString(), eligibility.ValidUntil, eligibility.RejectionReason));
    }
}
