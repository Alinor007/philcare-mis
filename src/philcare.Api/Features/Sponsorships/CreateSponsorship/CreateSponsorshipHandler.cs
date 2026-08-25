using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Sponsorships.Domain;

namespace philcare.Api.Features.Sponsorships.CreateSponsorship;

public sealed class CreateSponsorshipHandler(AppDbContext db)
{
    public async Task<Result<CreateSponsorshipResponse>> HandleAsync(CreateSponsorshipRequest request, CancellationToken cancellationToken)
    {
        var donor = await db.Donors.FirstOrDefaultAsync(d => d.Id == request.DonorId, cancellationToken);

        if (donor is null)
        {
            return Result.Failure<CreateSponsorshipResponse>(Error.NotFound("Sponsorships.DonorNotFound", "Donor not found."));
        }

        if (!donor.IsActive)
        {
            return Result.Failure<CreateSponsorshipResponse>(
                Error.Validation("Sponsorships.DonorInactive", "Cannot create a sponsorship for an inactive donor."));
        }

        var beneficiary = await db.Beneficiaries.FirstOrDefaultAsync(p => p.Id == request.BeneficiaryId, cancellationToken);

        if (beneficiary is null)
        {
            return Result.Failure<CreateSponsorshipResponse>(Error.NotFound("Sponsorships.BeneficiaryNotFound", "Beneficiary not found."));
        }

        if (!beneficiary.IsActive)
        {
            return Result.Failure<CreateSponsorshipResponse>(
                Error.Validation("Sponsorships.BeneficiaryInactive", "Cannot create a sponsorship for an inactive beneficiary."));
        }

        // Friendly pre-check for the common case. The actual guarantee is the unique index on
        // (DonorId, BeneficiaryId, IsActiveSponsorship) — this check alone is check-then-act and
        // two concurrent requests can both pass it. See the catch below.
        var duplicateActive = await db.Sponsorships.AnyAsync(
            s => s.DonorId == request.DonorId && s.BeneficiaryId == request.BeneficiaryId && s.Status != SponsorshipStatus.Ended,
            cancellationToken);

        if (duplicateActive)
        {
            return Result.Failure<CreateSponsorshipResponse>(
                Error.Conflict("Sponsorships.DuplicateActive", "This donor already has an active or paused sponsorship for this beneficiary."));
        }

        var sponsorship = new Sponsorship
        {
            DonorId = request.DonorId,
            BeneficiaryId = request.BeneficiaryId,
            SponsorshipType = request.SponsorshipType,
            MonthlyAmountPhp = request.MonthlyAmountPhp,
            StartDate = request.StartDate,
            Status = SponsorshipStatus.Active,
            CaseWorker = request.CaseWorker,
            NextReviewDate = request.NextReviewDate,
            Notes = request.Notes,
            IsActiveSponsorship = true
        };

        db.Sponsorships.Add(sponsorship);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique index caught a concurrent create the AnyAsync above missed — lose cleanly
            // with the same conflict the pre-check would have returned, not a raw 500.
            return Result.Failure<CreateSponsorshipResponse>(
                Error.Conflict("Sponsorships.DuplicateActive", "This donor already has an active or paused sponsorship for this beneficiary."));
        }

        return Result.Success(new CreateSponsorshipResponse(
            sponsorship.Id, sponsorship.DonorId, sponsorship.BeneficiaryId, sponsorship.SponsorshipType,
            sponsorship.MonthlyAmountPhp, sponsorship.Status.ToString()));
    }
}
