using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Donations.VoidDonation;

public sealed class VoidDonationHandler(AppDbContext db)
{
    public async Task<Result> HandleAsync(int id, CancellationToken cancellationToken)
    {
        var donation = await db.Donations
            .Include(d => d.Allocation)
            .ThenInclude(a => a!.FundBucket)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (donation is null)
        {
            return Result.Failure(Error.NotFound("Donations.NotFound", "Donation not found."));
        }

        if (donation.IsVoided)
        {
            return Result.Failure(Error.Conflict("Donations.AlreadyVoided", "This donation has already been voided."));
        }

        var allocation = donation.Allocation;

        if (allocation is not null)
        {
            var bucket = allocation.FundBucket;
            var remainingProgramAllocated = bucket.ProgramAllocated - allocation.ProgramAmount;

            if (remainingProgramAllocated < bucket.TotalExpensed)
            {
                return Result.Failure(Error.Validation(
                    "Donations.FundsAlreadySpent",
                    "Cannot void this donation because its funds have already been spent from the bucket."));
            }

            bucket.TotalReceived -= donation.Amount;
            bucket.AdminAllocated -= allocation.AdminAmount;
            bucket.ProgramAllocated -= allocation.ProgramAmount;
        }

        donation.IsVoided = true;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
