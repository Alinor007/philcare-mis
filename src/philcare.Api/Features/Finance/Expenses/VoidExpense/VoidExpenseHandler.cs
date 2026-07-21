using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Expenses.VoidExpense;

public sealed class VoidExpenseHandler(AppDbContext db)
{
    public async Task<Result> HandleAsync(int id, CancellationToken cancellationToken)
    {
        var expense = await db.Expenses
            .Include(e => e.FundingBucket)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (expense is null)
        {
            return Result.Failure(Error.NotFound("Expenses.NotFound", "Expense not found."));
        }

        if (expense.IsVoided)
        {
            return Result.Failure(Error.Conflict("Expenses.AlreadyVoided", "This expense has already been voided."));
        }

        expense.IsVoided = true;
        expense.FundingBucket.ExpensedAmount -= expense.AmountPhp;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
