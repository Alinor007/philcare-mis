using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Reports.GetFundBalances;

public sealed record FundBalanceRow(
    string FundCode, string FundName, bool IsRestricted, decimal OpeningBalancePhp,
    decimal DonationsPhp, decimal OtherIncomePhp, decimal ExpensesPhp, decimal ClosingBalancePhp, string CashStatus);

public sealed record FundBalancesResponse(
    int Year, List<FundBalanceRow> Funds,
    decimal TotalOpeningPhp, decimal TotalInPhp, decimal TotalExpensesPhp, decimal TotalClosingPhp);

/// <summary>
/// Per-fund opening/in/out/closing for the year — the org's Fund_Balances workbook sheet,
/// generalized to every fund rather than GetRestrictedFundLedger's restricted-only scope.
/// Deliberately re-derived from Donations/OtherIncomes/Expenses rather than read off
/// FundingBucket.AllocatedAmount/ExpensedAmount, which are all-time running totals with no
/// date dimension and so cannot answer "for this year".
/// </summary>
public sealed class GetFundBalancesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/fund-balances", async (int? year, AppDbContext db, CancellationToken ct) =>
        {
            var reportingYear = year ?? DateTime.UtcNow.Year;
            var yearStart = new DateTime(reportingYear, 1, 1);
            var yearEnd = yearStart.AddYears(1);

            var funds = await db.Funds.OrderBy(f => f.Code).ToListAsync(ct);

            var openings = await db.OpeningBalances
                .Where(o => o.Year == reportingYear)
                .ToDictionaryAsync(o => o.FundCode, o => o.OpeningBalancePhp, ct);

            // Materialize flat rows, then group in-memory (EF GroupBy translation lesson from Sprint 2).
            var donations = await db.Donations
                .Where(d => !d.IsVoided && d.DateReceived >= yearStart && d.DateReceived < yearEnd)
                .Select(d => new { d.FundCode, d.AmountPhp })
                .ToListAsync(ct);

            var otherIncome = await db.OtherIncomes
                .Where(i => !i.IsVoided && i.DateReceived >= yearStart && i.DateReceived < yearEnd)
                .Select(i => new { i.FundCode, i.AmountPhp })
                .ToListAsync(ct);

            var expenses = await db.Expenses
                .Where(e => !e.IsVoided && e.ExpenseDate >= yearStart && e.ExpenseDate < yearEnd)
                .Select(e => new { e.FundCode, e.AmountPhp })
                .ToListAsync(ct);

            var rows = new List<FundBalanceRow>(funds.Count);
            decimal totalOpening = 0, totalIn = 0, totalExpenses = 0, totalClosing = 0;

            foreach (var fund in funds)
            {
                var opening = openings.GetValueOrDefault(fund.Code, 0m);
                var fundDonations = donations.Where(d => d.FundCode == fund.Code).Sum(d => d.AmountPhp);
                var fundOtherIncome = otherIncome.Where(i => i.FundCode == fund.Code).Sum(i => i.AmountPhp);
                var fundExpenses = expenses.Where(e => e.FundCode == fund.Code).Sum(e => e.AmountPhp);
                var closing = opening + fundDonations + fundOtherIncome - fundExpenses;

                var cashStatus = closing < 0 ? "Review deficit" : closing == 0 ? "Zero" : "Available";

                rows.Add(new FundBalanceRow(
                    fund.Code, fund.Name, fund.IsRestricted, opening, fundDonations, fundOtherIncome, fundExpenses, closing, cashStatus));

                totalOpening += opening;
                totalIn += fundDonations + fundOtherIncome;
                totalExpenses += fundExpenses;
                totalClosing += closing;
            }

            return Results.Ok(new FundBalancesResponse(reportingYear, rows, totalOpening, totalIn, totalExpenses, totalClosing));
        })
        .WithName("GetFundBalances")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
