using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Donors.ReviewDonorKyd;

/// <summary>
/// Focused KYD status transition — one field, one action — so a reviewer isn't forced through the
/// general donor edit form (which requires re-submitting name/type/contact/etc just to flip a
/// compliance flag). Mirrors ChangeSponsorshipStatus's shape.
///
/// No fixed transition table, unlike Sponsorship's Active/Paused/Ended: KYD status is a compliance
/// assessment that can legitimately move any direction (a Cleared donor can be sent back to Review
/// if new information surfaces), so the only guard is "don't no-op".
/// </summary>
public sealed class ReviewDonorKydHandler(AppDbContext db)
{
    public async Task<Result<ReviewDonorKydResponse>> HandleAsync(int id, ReviewDonorKydRequest request, CancellationToken cancellationToken)
    {
        var donor = await db.Donors.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (donor is null)
        {
            return Result.Failure<ReviewDonorKydResponse>(Error.NotFound("Donors.NotFound", "Donor not found."));
        }

        if (donor.KydStatus == request.Status)
        {
            return Result.Failure<ReviewDonorKydResponse>(
                Error.Conflict("Donors.KydStatusUnchanged", $"This donor's KYD status is already {request.Status}."));
        }

        donor.KydStatus = request.Status;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ReviewDonorKydResponse(donor.Id, donor.KydStatus.ToString()));
    }
}
