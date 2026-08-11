using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Programs.Distributions.VoidDistribution;

public sealed class VoidDistributionHandler(AppDbContext db)
{
    public async Task<Result> HandleAsync(int id, CancellationToken cancellationToken)
    {
        var distribution = await db.Distributions
            .Include(d => d.Expense)
            .ThenInclude(e => e!.FundingBucket)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (distribution is null)
        {
            return Result.Failure(Error.NotFound("Distributions.NotFound", "Distribution not found."));
        }

        if (distribution.IsVoided)
        {
            return Result.Failure(Error.Conflict("Distributions.AlreadyVoided", "This distribution has already been voided."));
        }

        distribution.IsVoided = true;

        // Historical / zero-value distributions have no linked expense — nothing to reverse.
        // ExpensePosting.Reverse itself no-ops if the expense is already voided, so this stays
        // safe even if VoidExpense somehow ran first (blocked by Expenses.VoidViaDistribution,
        // but the guard costs nothing to keep here too).
        if (distribution.Expense is not null)
        {
            ExpensePosting.Reverse(distribution.Expense, distribution.Expense.FundingBucket);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
