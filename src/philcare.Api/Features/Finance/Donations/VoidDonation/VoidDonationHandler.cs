using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Donations.VoidDonation;

public sealed class VoidDonationHandler(AppDbContext db)
{
    public async Task<Result> HandleAsync(int id, CancellationToken cancellationToken)
    {
        var donation = await db.Donations
            .Include(d => d.Allocations)
            .ThenInclude(a => a.TargetBucket)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (donation is null)
        {
            return Result.Failure(Error.NotFound("Donations.NotFound", "Donation not found."));
        }

        if (donation.IsVoided)
        {
            return Result.Failure(Error.Conflict("Donations.AlreadyVoided", "This donation has already been voided."));
        }

        foreach (var allocation in donation.Allocations)
        {
            var bucket = allocation.TargetBucket;
            var remainingAfterVoid = bucket.AllocatedAmount - allocation.AllocatedAmountPhp;

            if (remainingAfterVoid < bucket.ExpensedAmount)
            {
                return Result.Failure(Error.Validation(
                    "Donations.FundsAlreadySpent",
                    $"Cannot void this donation because bucket '{bucket.Code}' has already spent funds from this allocation."));
            }
        }

        foreach (var allocation in donation.Allocations)
        {
            allocation.TargetBucket.AllocatedAmount -= allocation.AllocatedAmountPhp;
        }

        donation.IsVoided = true;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
