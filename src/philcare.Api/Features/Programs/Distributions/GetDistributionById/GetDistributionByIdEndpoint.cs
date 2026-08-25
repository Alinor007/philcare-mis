using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Distributions.GetDistributionById;

public sealed record DistributionDetailResponse(
    int Id,
    string DistributionType,
    int? ActivityId,
    string? FundingBucketCode,
    int Quantity,
    decimal UnitValuePhp,
    decimal TotalValuePhp,
    int BeneficiaryCount,
    DateTime DistributionDate,
    string? Location,
    bool FieldVerified,
    bool ReceivedConfirmation,
    string? ProcessedBy,
    string? ZakatAsnaf,
    string? Notes,
    bool IsVoided,
    // Null for pre-Sprint-6 rows and for zero-value in-kind handouts, which post no expense.
    // The UI uses it to link through to the ledger entry and to explain why a direct expense
    // void is blocked (409 Expenses.VoidViaDistribution).
    int? ExpenseId);

public sealed class GetDistributionByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/distributions/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var distribution = await db.Distributions
                .Where(d => d.Id == id)
                .Select(d => new DistributionDetailResponse(
                    d.Id, d.DistributionType, d.ActivityId, d.FundingBucketCode,
                    d.Quantity, d.UnitValuePhp, d.TotalValuePhp, d.BeneficiaryCount, d.DistributionDate, d.Location,
                    d.FieldVerified, d.ReceivedConfirmation, d.ProcessedBy, d.ZakatAsnaf, d.Notes, d.IsVoided, d.ExpenseId))
                .FirstOrDefaultAsync(ct);

            if (distribution is null)
            {
                return Results.Problem(title: "Distributions.NotFound", detail: "Distribution not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(distribution);
        })
        .WithName("GetDistributionById")
        .WithTags("Distributions")
        .RequireAuthorization();
    }
}
