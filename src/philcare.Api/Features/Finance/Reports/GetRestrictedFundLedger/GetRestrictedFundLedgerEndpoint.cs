using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Reports.GetRestrictedFundLedger;

public sealed record RestrictedLedgerEntry(
    DateTime Date, string TransactionType, string Reference, decimal AmountIn, decimal AmountOut, decimal RunningBalance, string? Notes);

public sealed record RestrictedFundLedgerResponse(
    string FundCode, string FundName, decimal OpeningBalancePhp, List<RestrictedLedgerEntry> Entries, decimal ClosingBalancePhp);

public sealed class GetRestrictedFundLedgerEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/restricted-ledger", async (string? fundCode, int? year, AppDbContext db, CancellationToken ct) =>
        {
            var reportingYear = year ?? DateTime.UtcNow.Year;

            var fundsQuery = db.Funds.Where(f => f.IsRestricted);
            if (!string.IsNullOrWhiteSpace(fundCode))
            {
                fundsQuery = fundsQuery.Where(f => f.Code == fundCode);
            }

            var funds = await fundsQuery.OrderBy(f => f.Code).ToListAsync(ct);
            var results = new List<RestrictedFundLedgerResponse>();

            foreach (var fund in funds)
            {
                var opening = await db.OpeningBalances
                    .Where(o => o.FundCode == fund.Code && o.Year == reportingYear)
                    .Select(o => o.OpeningBalancePhp)
                    .FirstOrDefaultAsync(ct);

                var allocationsIn = await db.Allocations
                    .Where(a => a.SourceFundCode == fund.Code && a.ReportingYear == reportingYear)
                    .Select(a => new { Date = a.AllocationDate, Ref = a.DonationId != null ? $"DON-{a.DonationId}" : "Opening", Amount = a.AllocatedAmountPhp, a.EvidenceNotes })
                    .ToListAsync(ct);

                var expensesOut = await db.Expenses
                    .Where(e => e.FundCode == fund.Code && !e.IsVoided && e.ExpenseDate.Year == reportingYear)
                    .Select(e => new { Date = e.ExpenseDate, Ref = $"EXP-{e.Id}", Amount = e.AmountPhp, e.Description })
                    .ToListAsync(ct);

                var combined = allocationsIn
                    .Select(a => (a.Date, Type: "Allocation", a.Ref, In: a.Amount, Out: 0m, Notes: a.EvidenceNotes))
                    .Concat(expensesOut.Select(e => (e.Date, Type: "Expense", e.Ref, In: 0m, Out: e.Amount, Notes: (string?)e.Description)))
                    .OrderBy(tx => tx.Date)
                    .ToList();

                var running = opening;
                var entries = new List<RestrictedLedgerEntry>();

                foreach (var tx in combined)
                {
                    running += tx.In - tx.Out;
                    entries.Add(new RestrictedLedgerEntry(tx.Date, tx.Type, tx.Ref, tx.In, tx.Out, running, tx.Notes));
                }

                results.Add(new RestrictedFundLedgerResponse(fund.Code, fund.Name, opening, entries, running));
            }

            return Results.Ok(results);
        })
        .WithName("GetRestrictedFundLedger")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
