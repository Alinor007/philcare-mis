using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Sponsorships.Domain;

namespace philcare.Api.Features.Sponsorships.ChangeSponsorshipStatus;

public sealed class ChangeSponsorshipStatusHandler(AppDbContext db)
{
    private static readonly Dictionary<SponsorshipStatus, SponsorshipStatus[]> AllowedTransitions = new()
    {
        [SponsorshipStatus.Active] = [SponsorshipStatus.Paused, SponsorshipStatus.Ended],
        [SponsorshipStatus.Paused] = [SponsorshipStatus.Active, SponsorshipStatus.Ended],
        [SponsorshipStatus.Ended] = []
    };

    public async Task<Result<ChangeSponsorshipStatusResponse>> HandleAsync(
        int id, ChangeSponsorshipStatusRequest request, CancellationToken cancellationToken)
    {
        var sponsorship = await db.Sponsorships.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sponsorship is null)
        {
            return Result.Failure<ChangeSponsorshipStatusResponse>(Error.NotFound("Sponsorships.NotFound", "Sponsorship not found."));
        }

        if (sponsorship.Status == SponsorshipStatus.Ended)
        {
            return Result.Failure<ChangeSponsorshipStatusResponse>(
                Error.Conflict("Sponsorships.AlreadyEnded", "This sponsorship has already ended."));
        }

        if (!AllowedTransitions[sponsorship.Status].Contains(request.Status))
        {
            return Result.Failure<ChangeSponsorshipStatusResponse>(
                Error.Conflict("Sponsorships.InvalidTransition", $"Cannot transition from {sponsorship.Status} to {request.Status}."));
        }

        sponsorship.Status = request.Status;

        if (request.Status == SponsorshipStatus.Ended)
        {
            sponsorship.EndDate = request.EndDate ?? DateTime.UtcNow.Date;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ChangeSponsorshipStatusResponse(sponsorship.Id, sponsorship.Status.ToString(), sponsorship.EndDate));
    }
}
