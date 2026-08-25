using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Sponsorships.Domain;

namespace philcare.Api.Features.Sponsorships.UpdateSponsorship;

public sealed class UpdateSponsorshipHandler(AppDbContext db)
{
    public async Task<Result<UpdateSponsorshipResponse>> HandleAsync(int id, UpdateSponsorshipRequest request, CancellationToken cancellationToken)
    {
        var sponsorship = await db.Sponsorships.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sponsorship is null)
        {
            return Result.Failure<UpdateSponsorshipResponse>(Error.NotFound("Sponsorships.NotFound", "Sponsorship not found."));
        }

        if (sponsorship.Status == SponsorshipStatus.Ended)
        {
            return Result.Failure<UpdateSponsorshipResponse>(
                Error.Conflict("Sponsorships.AlreadyEnded", "This sponsorship has ended and can no longer be updated."));
        }

        sponsorship.SponsorshipType = request.SponsorshipType;
        sponsorship.MonthlyAmountPhp = request.MonthlyAmountPhp;
        sponsorship.CaseWorker = request.CaseWorker;
        sponsorship.NextReviewDate = request.NextReviewDate;
        sponsorship.Notes = request.Notes;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateSponsorshipResponse(
            sponsorship.Id, sponsorship.DonorId, sponsorship.BeneficiaryId, sponsorship.SponsorshipType,
            sponsorship.MonthlyAmountPhp, sponsorship.Status.ToString()));
    }
}
