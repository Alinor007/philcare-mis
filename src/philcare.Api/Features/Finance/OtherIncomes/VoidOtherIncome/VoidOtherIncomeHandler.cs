using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.OtherIncomes.VoidOtherIncome;

public sealed class VoidOtherIncomeHandler(AppDbContext db)
{
    public async Task<Result> HandleAsync(int id, CancellationToken cancellationToken)
    {
        var income = await db.OtherIncomes
            .Include(i => i.Allocations)
            .ThenInclude(a => a.TargetBucket)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (income is null)
        {
            return Result.Failure(Error.NotFound("OtherIncome.NotFound", "Other income record not found."));
        }

        if (income.IsVoided)
        {
            return Result.Failure(Error.Conflict("OtherIncome.AlreadyVoided", "This income record has already been voided."));
        }

        foreach (var allocation in income.Allocations)
        {
            var bucket = allocation.TargetBucket;
            var remainingAfterVoid = bucket.AllocatedAmount - allocation.AllocatedAmountPhp;

            if (remainingAfterVoid < bucket.ExpensedAmount)
            {
                return Result.Failure(Error.Validation(
                    "OtherIncome.FundsAlreadySpent",
                    $"Cannot void this income record because bucket '{bucket.Code}' has already spent funds from this allocation."));
            }
        }

        foreach (var allocation in income.Allocations)
        {
            allocation.TargetBucket.AllocatedAmount -= allocation.AllocatedAmountPhp;
        }

        income.IsVoided = true;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
