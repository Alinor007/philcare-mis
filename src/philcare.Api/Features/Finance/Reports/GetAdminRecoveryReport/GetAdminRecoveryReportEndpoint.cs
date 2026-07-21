using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Reports.GetAdminRecoveryReport;

public sealed record AdminRecoveryRow(
    string BucketCode, string BucketName, decimal PolicyCapRate, decimal AllocatedAmountPhp, decimal ExpensedAmountPhp,
    decimal RemainingAmountPhp, string Status);

public sealed record AdminRecoveryReportResponse(
    List<AdminRecoveryRow> Buckets, decimal TotalAllocatedPhp, decimal TotalExpensedPhp, decimal TotalRemainingPhp);

public sealed class GetAdminRecoveryReportEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/admin-recovery", async (AppDbContext db, CancellationToken ct) =>
        {
            var buckets = await db.FundingBuckets
                .Where(b => b.BucketType == BucketType.Admin)
                .OrderBy(b => b.Code)
                .ToListAsync(ct);

            var rows = buckets.Select(b =>
            {
                var remaining = b.AllocatedAmount - b.ExpensedAmount;
                var status = remaining < 0
                    ? "Overspent"
                    : (b.AllocatedAmount == 0 && b.ExpensedAmount == 0 ? "No spend" : "OK");

                return new AdminRecoveryRow(b.Code, b.Name, b.MaxAdminRate, b.AllocatedAmount, b.ExpensedAmount, remaining, status);
            }).ToList();

            var response = new AdminRecoveryReportResponse(
                rows, rows.Sum(r => r.AllocatedAmountPhp), rows.Sum(r => r.ExpensedAmountPhp), rows.Sum(r => r.RemainingAmountPhp));

            return Results.Ok(response);
        })
        .WithName("GetAdminRecoveryReport")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
