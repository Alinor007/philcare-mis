using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Donations.GetDonationById;

public sealed record DonationAllocationLineResponse(
    AllocationType AllocationType, string TargetBucketCode, decimal AllocationRate, decimal AllocatedAmountPhp);

public sealed record DonationEmailStatusResponse(
    int Id,
    EmailType EmailType,
    EmailDeliveryStatus Status,
    string ToEmail,
    int AttemptCount,
    DateTime? SentAt,
    string? LastError,
    DateTime CreatedAt);

public sealed record DonationDetailResponse(
    int Id,
    int DonorId,
    string DonorName,
    decimal AmountOriginal,
    string Currency,
    decimal FxRateToPhp,
    decimal AmountPhp,
    string FundCode,
    DateTime DateReceived,
    string Channel,
    string? Purpose,
    string? ReceiptNo,
    string? TransactionRef,
    bool AdminAllowed,
    decimal AdminRateInput,
    decimal AdminRateCap,
    decimal AdminRateApplied,
    decimal ProgramAllocationPhp,
    decimal AdminAllocationPhp,
    string? Notes,
    bool IsVoided,
    List<DonationAllocationLineResponse> Allocations,
    List<DonationEmailStatusResponse> Emails);

public sealed class GetDonationByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/donations/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var donation = await db.Donations
                .Include(d => d.Donor)
                .Include(d => d.Allocations)
                .Include(d => d.OutboxEmails)
                .FirstOrDefaultAsync(d => d.Id == id, ct);

            if (donation is null)
            {
                return Results.Problem(title: "Donations.NotFound", detail: "Donation not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var response = new DonationDetailResponse(
                donation.Id, donation.DonorId, donation.Donor.Name, donation.AmountOriginal, donation.Currency,
                donation.FxRateToPhp, donation.AmountPhp, donation.FundCode, donation.DateReceived, donation.Channel,
                donation.Purpose, donation.ReceiptNo, donation.TransactionRef, donation.AdminAllowed, donation.AdminRateInput, donation.AdminRateCap, donation.AdminRateApplied,
                donation.ProgramAllocationPhp, donation.AdminAllocationPhp, donation.Notes, donation.IsVoided,
                donation.Allocations
                    .Select(a => new DonationAllocationLineResponse(a.AllocationType, a.TargetBucketCode, a.AllocationRate, a.AllocatedAmountPhp))
                    .ToList(),
                donation.OutboxEmails
                    .OrderByDescending(e => e.CreatedAt)
                    .Select(e => new DonationEmailStatusResponse(e.Id, e.EmailType, e.Status, e.ToEmail, e.AttemptCount, e.SentAt, e.LastError, e.CreatedAt))
                    .ToList());

            return Results.Ok(response);
        })
        .WithName("GetDonationById")
        .WithTags("Donations")
        .RequireAuthorization();
    }
}
