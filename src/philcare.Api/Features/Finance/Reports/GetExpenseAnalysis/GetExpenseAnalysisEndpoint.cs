using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Reports.GetExpenseAnalysis;

public sealed record ExpenseCategoryRow(
    string ExpenseCategory, decimal AmountPhp, int Transactions, decimal PercentOfTotal,
    int BeneficiaryCount, decimal? AverageCostPerBeneficiary);

public sealed record ExpenseProgramRow(
    string ProgramOrProject, decimal AmountPhp, int Transactions, int BeneficiaryCount, decimal? AverageCostPerBeneficiary);

/// <summary>Program vs. admin vs. operations vs. capital split, by FundingBucket.BucketType.</summary>
public sealed record ExpenseFunctionRow(string BucketType, decimal AmountPhp, decimal PercentOfTotal);

public sealed record ExpenseAnalysisResponse(
    int Year, List<ExpenseCategoryRow> ByCategory, List<ExpenseProgramRow> ByProgram,
    List<ExpenseFunctionRow> ByFunction, decimal GrandTotalPhp, int TotalTransactions);

/// <summary>
/// Spend broken down three ways for the year — the org's Expense_Program_Summary_2026 and
/// Beneficiary_Report_2026 workbook sheets combined. ByFunction is keyed on
/// FundingBucket.BucketType (joined via Expense.FundingBucketCode), not Expense.ExpenseFunction:
/// that column is free text with no seeded lookup vocabulary and nothing populates it, so
/// BucketType is the only reliable program/admin split that actually exists in the data.
/// </summary>
public sealed class GetExpenseAnalysisEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/expense-analysis", async (int? year, AppDbContext db, CancellationToken ct) =>
        {
            var reportingYear = year ?? DateTime.UtcNow.Year;
            var yearStart = new DateTime(reportingYear, 1, 1);
            var yearEnd = yearStart.AddYears(1);

            // Materialize flat rows, then group in-memory (EF GroupBy translation lesson from Sprint 2).
            var expenses = await db.Expenses
                .Where(e => !e.IsVoided && e.ExpenseDate >= yearStart && e.ExpenseDate < yearEnd)
                .Select(e => new
                {
                    e.ExpenseCategory,
                    e.ProgramOrProject,
                    e.AmountPhp,
                    e.BeneficiaryCount,
                    BucketType = e.FundingBucket.BucketType,
                })
                .ToListAsync(ct);

            var grandTotal = expenses.Sum(e => e.AmountPhp);
            var totalTransactions = expenses.Count;

            var byCategory = expenses
                .GroupBy(e => e.ExpenseCategory)
                .Select(g =>
                {
                    var amount = g.Sum(e => e.AmountPhp);
                    var beneficiaries = g.Sum(e => e.BeneficiaryCount ?? 0);
                    return new ExpenseCategoryRow(
                        g.Key, amount, g.Count(),
                        grandTotal == 0 ? 0 : Math.Round(amount / grandTotal, 4),
                        beneficiaries,
                        beneficiaries == 0 ? null : Math.Round(amount / beneficiaries, 2));
                })
                .OrderByDescending(r => r.AmountPhp)
                .ToList();

            var byProgram = expenses
                .GroupBy(e => string.IsNullOrWhiteSpace(e.ProgramOrProject) ? "Unassigned" : e.ProgramOrProject!)
                .Select(g =>
                {
                    var amount = g.Sum(e => e.AmountPhp);
                    var beneficiaries = g.Sum(e => e.BeneficiaryCount ?? 0);
                    return new ExpenseProgramRow(
                        g.Key, amount, g.Count(), beneficiaries,
                        beneficiaries == 0 ? null : Math.Round(amount / beneficiaries, 2));
                })
                .OrderByDescending(r => r.AmountPhp)
                .ToList();

            var byFunction = expenses
                .GroupBy(e => e.BucketType.ToString())
                .Select(g => new ExpenseFunctionRow(
                    g.Key, g.Sum(e => e.AmountPhp),
                    grandTotal == 0 ? 0 : Math.Round(g.Sum(e => e.AmountPhp) / grandTotal, 4)))
                .OrderByDescending(r => r.AmountPhp)
                .ToList();

            return Results.Ok(new ExpenseAnalysisResponse(reportingYear, byCategory, byProgram, byFunction, grandTotal, totalTransactions));
        })
        .WithName("GetExpenseAnalysis")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
