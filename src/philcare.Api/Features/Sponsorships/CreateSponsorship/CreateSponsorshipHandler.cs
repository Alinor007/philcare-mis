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

        var participant = await db.Participants.FirstOrDefaultAsync(p => p.Id == request.ParticipantId, cancellationToken);

        if (participant is null)
        {
            return Result.Failure<CreateSponsorshipResponse>(Error.NotFound("Sponsorships.ParticipantNotFound", "Participant not found."));
        }

        if (!participant.IsActive)
        {
            return Result.Failure<CreateSponsorshipResponse>(
                Error.Validation("Sponsorships.ParticipantInactive", "Cannot create a sponsorship for an inactive participant."));
        }

        var duplicateActive = await db.Sponsorships.AnyAsync(
            s => s.DonorId == request.DonorId && s.ParticipantId == request.ParticipantId && s.Status != SponsorshipStatus.Ended,
            cancellationToken);

        if (duplicateActive)
        {
            return Result.Failure<CreateSponsorshipResponse>(
                Error.Conflict("Sponsorships.DuplicateActive", "This donor already has an active or paused sponsorship for this participant."));
        }

        var sponsorship = new Sponsorship
        {
            DonorId = request.DonorId,
            ParticipantId = request.ParticipantId,
            SponsorshipType = request.SponsorshipType,
            MonthlyAmountPhp = request.MonthlyAmountPhp,
            StartDate = request.StartDate,
            Status = SponsorshipStatus.Active,
            CaseWorker = request.CaseWorker,
            NextReviewDate = request.NextReviewDate,
            Notes = request.Notes
        };

        db.Sponsorships.Add(sponsorship);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateSponsorshipResponse(
            sponsorship.Id, sponsorship.DonorId, sponsorship.ParticipantId, sponsorship.SponsorshipType,
            sponsorship.MonthlyAmountPhp, sponsorship.Status.ToString()));
    }
}
