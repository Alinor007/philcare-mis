using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Reports.GetCashFlow;

public sealed record CashFlowMonthRow(
    int Month, decimal OpeningBalancePhp, decimal DonationsInPhp, decimal OtherIncomeInPhp, decimal TotalInPhp,
    decimal ExpensesOutPhp, decimal NetMovementPhp, decimal ClosingBalancePhp,
    decimal RestrictedClosingPhp, decimal ZakatClosingPhp);

public sealed record CashFlowResponse(
    int Year, decimal OpeningBalancePhp, List<CashFlowMonthRow> Months,
    decimal TotalInPhp, decimal TotalOutPhp, decimal ClosingBalancePhp);

/// <summary>
/// Fund-movement statement in the shape of the org's own Cash_Flow_2026 workbook sheet: monthly
/// opening + donations + other income - expenses = closing, rolled forward across the year, plus
/// a running closing balance narrowed to restricted funds and to the Zakat fund specifically.
///
/// This is NOT a reconciled bank/cash position — there is no bank account entity anywhere in the
/// system (see SYSTEM_REQUIREMENTS.md). It is fund movement only, same caveat the workbook itself
/// notes ("Bank_Reconciliation" is a separate, manually-completed sheet there).
///
/// Opening balance is annual only (OpeningBalances is keyed Year+FundCode, no sub-annual entry
/// point), so this report is necessarily a calendar-year report.
/// </summary>
public sealed class GetCashFlowEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/cash-flow", async (int? year, AppDbContext db, CancellationToken ct) =>
        {
            var reportingYear = year ?? DateTime.UtcNow.Year;
            var yearStart = new DateTime(reportingYear, 1, 1);
            var yearEnd = yearStart.AddYears(1);

            var openingTotal = await db.OpeningBalances
                .Where(o => o.Year == reportingYear)
                .SumAsync(o => (decimal?)o.OpeningBalancePhp, ct) ?? 0m;

            var restrictedFundCodes = await db.Funds
                .Where(f => f.IsRestricted)
                .Select(f => f.Code)
                .ToListAsync(ct);

            var restrictedOpening = await db.OpeningBalances
                .Where(o => o.Year == reportingYear && restrictedFundCodes.Contains(o.FundCode))
                .SumAsync(o => (decimal?)o.OpeningBalancePhp, ct) ?? 0m;

            var zakatOpening = await db.OpeningBalances
                .Where(o => o.Year == reportingYear && o.FundCode == FinanceRules.ZakatFundCode)
                .SumAsync(o => (decimal?)o.OpeningBalancePhp, ct) ?? 0m;

            // Materialize flat rows, then group in-memory (EF GroupBy translation lesson from Sprint 2).
            var donations = await db.Donations
                .Where(d => !d.IsVoided && d.DateReceived >= yearStart && d.DateReceived < yearEnd)
                .Select(d => new { d.DateReceived, d.AmountPhp, d.FundCode })
                .ToListAsync(ct);

            var otherIncome = await db.OtherIncomes
                .Where(i => !i.IsVoided && i.DateReceived >= yearStart && i.DateReceived < yearEnd)
                .Select(i => new { i.DateReceived, i.AmountPhp, i.FundCode })
                .ToListAsync(ct);

            var expenses = await db.Expenses
                .Where(e => !e.IsVoided && e.ExpenseDate >= yearStart && e.ExpenseDate < yearEnd)
                .Select(e => new { e.ExpenseDate, e.AmountPhp, e.FundCode })
                .ToListAsync(ct);

            var today = DateTime.UtcNow;
            var lastMonth = reportingYear == today.Year ? today.Month : 12;

            var months = new List<CashFlowMonthRow>(lastMonth);
            var running = openingTotal;
            var restrictedRunning = restrictedOpening;
            var zakatRunning = zakatOpening;
            decimal totalIn = 0, totalOut = 0;

            for (var month = 1; month <= lastMonth; month++)
            {
                var monthDonations = donations.Where(d => d.DateReceived.Month == month).Sum(d => d.AmountPhp);
                var monthOtherIncome = otherIncome.Where(i => i.DateReceived.Month == month).Sum(i => i.AmountPhp);
                var monthExpenses = expenses.Where(e => e.ExpenseDate.Month == month).Sum(e => e.AmountPhp);
                var monthIn = monthDonations + monthOtherIncome;
                var net = monthIn - monthExpenses;

                var opening = running;
                running += net;
                totalIn += monthIn;
                totalOut += monthExpenses;

                var restrictedIn = donations.Where(d => d.DateReceived.Month == month && restrictedFundCodes.Contains(d.FundCode)).Sum(d => d.AmountPhp)
                    + otherIncome.Where(i => i.DateReceived.Month == month && restrictedFundCodes.Contains(i.FundCode)).Sum(i => i.AmountPhp);
                var restrictedOut = expenses.Where(e => e.ExpenseDate.Month == month && restrictedFundCodes.Contains(e.FundCode)).Sum(e => e.AmountPhp);
                restrictedRunning += restrictedIn - restrictedOut;

                var zakatIn = donations.Where(d => d.DateReceived.Month == month && d.FundCode == FinanceRules.ZakatFundCode).Sum(d => d.AmountPhp)
                    + otherIncome.Where(i => i.DateReceived.Month == month && i.FundCode == FinanceRules.ZakatFundCode).Sum(i => i.AmountPhp);
                var zakatOut = expenses.Where(e => e.ExpenseDate.Month == month && e.FundCode == FinanceRules.ZakatFundCode).Sum(e => e.AmountPhp);
                zakatRunning += zakatIn - zakatOut;

                months.Add(new CashFlowMonthRow(
                    month, opening, monthDonations, monthOtherIncome, monthIn, monthExpenses, net, running,
                    restrictedRunning, zakatRunning));
            }

            return Results.Ok(new CashFlowResponse(reportingYear, openingTotal, months, totalIn, totalOut, running));
        })
        .WithName("GetCashFlow")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
