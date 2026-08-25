using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Dashboard.GetDashboardSummary;

/// <summary>One slice of the expenses-by-category pie, scoped to the reporting year.</summary>
public sealed record ExpenseCategorySlice(string ExpenseCategory, decimal TotalPhp);

/// <summary>Donations received vs. expenses posted in one month of the reporting year.</summary>
public sealed record MonthlyTrendPoint(int Month, decimal DonationsPhp, decimal ExpensesPhp);

/// <summary>
/// Beneficiaries registered in a month, plus the running total across all years. `Cumulative`
/// starts from the count registered before the reporting year, so the growth curve doesn't drop
/// back to zero every January.
/// </summary>
public sealed record BeneficiaryGrowthPoint(int Month, int Registered, int Cumulative);

/// <summary>
/// Headline counters are all-time; the three series are scoped to <paramref name="Year"/> so the
/// pie and the two charts describe the same window.
/// </summary>
public sealed record DashboardSummaryResponse(
    int Year,
    int TotalBeneficiaries,
    int ActiveBeneficiaries,
    int TotalProjects,
    int ActiveProjects,
    int DonationCount,
    decimal TotalDonationsPhp,
    decimal YearDonationsPhp,
    decimal YearExpensesPhp,
    List<ExpenseCategorySlice> ExpensesByCategory,
    List<BeneficiaryGrowthPoint> BeneficiaryGrowth,
    List<MonthlyTrendPoint> MonthlyTrend);

public sealed class GetDashboardSummaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/dashboard-summary", async (int? year, AppDbContext db, CancellationToken ct) =>
        {
            var today = DateTime.UtcNow.Date;
            var reportingYear = year ?? today.Year;
            var yearStart = new DateTime(reportingYear, 1, 1);
            var yearEnd = yearStart.AddYears(1);

            // Year-to-date for the current year; a past year charts all twelve months.
            var lastMonth = reportingYear == today.Year ? today.Month : 12;

            var totalBeneficiaries = await db.Beneficiaries.CountAsync(ct);
            var activeBeneficiaries = await db.Beneficiaries.CountAsync(b => b.IsActive, ct);
            var totalProjects = await db.Projects.CountAsync(ct);
            var activeProjects = await db.Projects.CountAsync(p => p.IsActive, ct);

            var liveDonations = db.Donations.Where(d => !d.IsVoided);
            var donationCount = await liveDonations.CountAsync(ct);

            // Nullable sum then coalesce: SUM over no rows is NULL, which won't map onto `decimal`.
            var totalDonationsPhp = await liveDonations.SumAsync(d => (decimal?)d.AmountPhp, ct) ?? 0m;

            // Materialize flat rows, then group in-memory (EF GroupBy translation lesson from Sprint 2).
            var yearDonations = await liveDonations
                .Where(d => d.DateReceived >= yearStart && d.DateReceived < yearEnd)
                .Select(d => new { d.DateReceived, d.AmountPhp })
                .ToListAsync(ct);

            var yearExpenses = await db.Expenses
                .Where(e => !e.IsVoided && e.ExpenseDate >= yearStart && e.ExpenseDate < yearEnd)
                .Select(e => new { e.ExpenseDate, e.AmountPhp, e.ExpenseCategory })
                .ToListAsync(ct);

            var yearRegistrations = await db.Beneficiaries
                .Where(b => b.CreatedAt >= yearStart && b.CreatedAt < yearEnd)
                .Select(b => b.CreatedAt)
                .ToListAsync(ct);

            var registeredBeforeYear = await db.Beneficiaries.CountAsync(b => b.CreatedAt < yearStart, ct);

            var expensesByCategory = yearExpenses
                .GroupBy(e => e.ExpenseCategory)
                .Select(g => new ExpenseCategorySlice(g.Key, g.Sum(e => e.AmountPhp)))
                .OrderByDescending(r => r.TotalPhp)
                .ToList();

            // Every month up to `lastMonth` is emitted even when empty, so neither chart renders a gap.
            var monthlyTrend = new List<MonthlyTrendPoint>(lastMonth);
            var beneficiaryGrowth = new List<BeneficiaryGrowthPoint>(lastMonth);
            var cumulative = registeredBeforeYear;

            for (var month = 1; month <= lastMonth; month++)
            {
                monthlyTrend.Add(new MonthlyTrendPoint(
                    month,
                    yearDonations.Where(d => d.DateReceived.Month == month).Sum(d => d.AmountPhp),
                    yearExpenses.Where(e => e.ExpenseDate.Month == month).Sum(e => e.AmountPhp)));

                var registered = yearRegistrations.Count(c => c.Month == month);
                cumulative += registered;
                beneficiaryGrowth.Add(new BeneficiaryGrowthPoint(month, registered, cumulative));
            }

            return Results.Ok(new DashboardSummaryResponse(
                reportingYear,
                totalBeneficiaries,
                activeBeneficiaries,
                totalProjects,
                activeProjects,
                donationCount,
                totalDonationsPhp,
                yearDonations.Sum(d => d.AmountPhp),
                yearExpenses.Sum(e => e.AmountPhp),
                expensesByCategory,
                beneficiaryGrowth,
                monthlyTrend));
        })
        .WithName("GetDashboardSummary")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
