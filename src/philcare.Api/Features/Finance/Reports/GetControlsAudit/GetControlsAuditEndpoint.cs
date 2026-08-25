using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.Zakat.Domain;

namespace philcare.Api.Features.Finance.Reports.GetControlsAudit;

public sealed record ControlCheckRow(string Control, int Result, string Risk, string RecommendedAction, string Owner);

public sealed record ControlsAuditResponse(int Year, List<ControlCheckRow> Checks, int TotalExceptions, int CriticalCount);

/// <summary>
/// Automated pre-close exception list — the org's Controls_Audit workbook sheet, restricted to
/// checks with a real backing field (the workbook's version also covers bank reconciliation
/// differences and donations missing a donor id, neither of which this system can produce: every
/// donation requires a DonorId at the database level, and there is no bank-statement entity).
/// Each row's Result is a live count, not a point-in-time snapshot.
/// </summary>
public sealed class GetControlsAuditEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/controls-audit", async (int? year, AppDbContext db, CancellationToken ct) =>
        {
            var reportingYear = year ?? DateTime.UtcNow.Year;
            var yearStart = new DateTime(reportingYear, 1, 1);
            var yearEnd = yearStart.AddYears(1);
            var today = DateTime.UtcNow;

            var amlReview = await db.Donations.CountAsync(
                d => !d.IsVoided && d.AmlReviewFlag && d.DateReceived >= yearStart && d.DateReceived < yearEnd, ct);

            var unresolvedKyd = await db.Donors.CountAsync(
                d => d.IsActive && (d.KydStatus == KydStatus.Pending || d.KydStatus == KydStatus.Review), ct);

            var pendingApproval = await db.Expenses.CountAsync(
                e => !e.IsVoided && e.ApprovalStatus != "Approved" && e.ExpenseDate >= yearStart && e.ExpenseDate < yearEnd, ct);

            var cashOverThreshold = await db.Expenses.CountAsync(
                e => !e.IsVoided && e.PaymentMethod == "CASH" && e.AmountPhp > 10000m
                    && e.SupportingDocStatus != "Complete"
                    && e.ExpenseDate >= yearStart && e.ExpenseDate < yearEnd, ct);

            var overspentBuckets = await db.FundingBuckets.CountAsync(b => b.AllocatedAmount - b.ExpensedAmount < 0, ct);

            var zakatDonationTotal = await db.Donations
                .Where(d => !d.IsVoided && d.FundCode == FinanceRules.ZakatFundCode)
                .SumAsync(d => (decimal?)d.AmountPhp, ct) ?? 0m;
            var amilAllocated = await db.FundingBuckets
                .Where(b => b.Code == FinanceRules.ZakatAmilBucket)
                .Select(b => b.AllocatedAmount)
                .FirstOrDefaultAsync(ct);
            var amilCapExceeded = amilAllocated > FinanceRules.MaxAmilRate * zakatDonationTotal ? 1 : 0;

            var zakatMissingAsnaf = await db.Expenses.CountAsync(
                e => !e.IsVoided && e.FundingBucketCode == FinanceRules.ZakatProgramBucket && e.ZakatAsnaf == null
                    && e.ExpenseDate >= yearStart && e.ExpenseDate < yearEnd, ct);

            var noConsent = await db.Beneficiaries.CountAsync(b => b.IsActive && !b.ConsentOnFile, ct);

            var expiredApprovals = await db.ZakatEligibilities.CountAsync(
                z => z.Status == ZakatEligibilityStatus.Approved && z.ValidUntil != null && z.ValidUntil < today, ct);

            var missingReceipt = await db.Donations.CountAsync(
                d => !d.IsVoided && d.ReceiptNo == null && d.DateReceived >= yearStart && d.DateReceived < yearEnd, ct);

            var checks = new List<ControlCheckRow>
            {
                new("Donations requiring AML review", amlReview, "High",
                    "Review and document the KYD/AML decision for each flagged donation.", "Compliance"),
                new("Donors with unresolved KYD status", unresolvedKyd, "High",
                    "Complete the KYD review and clear or reject each donor.", "Compliance"),
                new("Expenses pending approval", pendingApproval, "High",
                    "Route to the approver of record; do not treat as posted until approved.", "Finance"),
                new("Cash payments over PHP 10,000 with incomplete documentation", cashOverThreshold, "High",
                    "Attach a signed receipt and approval; avoid cash where possible.", "Finance"),
                new("Overspent funding buckets", overspentBuckets, "High",
                    "Review source allocation or reclassify the expenses causing the deficit.", "Treasurer"),
                new("Zakat amil share above the 12.5% cap", amilCapExceeded, "Critical",
                    "Ensure amil expenses remain within 12.5% of zakat collected.", "Sharia/Finance"),
                new("Zakat program expenses missing an asnaf category", zakatMissingAsnaf, "High",
                    "Complete the zakat asnaf classification before closing the period.", "Finance"),
                new("Active beneficiaries without consent on file", noConsent, "High",
                    "Obtain and record consent, or deactivate the record.", "Program"),
                new("Zakat eligibilities approved but past their valid-until date", expiredApprovals, "Medium",
                    "Reassess eligibility before authorizing further zakat distributions.", "Finance"),
                new("Donations missing a receipt number", missingReceipt, "Medium",
                    "Investigate — receipt numbers are normally auto-assigned on creation.", "Finance"),
            };

            var totalExceptions = checks.Sum(c => c.Result);
            var criticalCount = checks.Where(c => c.Risk == "Critical" && c.Result > 0).Sum(c => c.Result);

            return Results.Ok(new ControlsAuditResponse(reportingYear, checks, totalExceptions, criticalCount));
        })
        .WithName("GetControlsAudit")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
