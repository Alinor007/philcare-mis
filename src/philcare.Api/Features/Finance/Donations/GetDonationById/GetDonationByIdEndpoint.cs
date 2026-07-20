using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Donations.GetDonationById;

public sealed record DonationAllocationResponse(decimal ProgramAmount, decimal AdminAmount, decimal AmilAmount);

public sealed record DonationDetailResponse(
    int Id,
    int DonorId,
    string DonorName,
    decimal Amount,
    string Currency,
    string FundType,
    DateTime ReceivedDate,
    string PaymentMethod,
    bool AdminAllowed,
    decimal AdminRate,
    decimal AmilRate,
    string? Reference,
    string? Notes,
    bool IsVoided,
    DonationAllocationResponse? Allocation);

public sealed class GetDonationByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/donations/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var donation = await db.Donations
                .Include(d => d.Donor)
                .Include(d => d.Allocation)
                .FirstOrDefaultAsync(d => d.Id == id, ct);

            if (donation is null)
            {
                return Results.Problem(title: "Donations.NotFound", detail: "Donation not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var response = new DonationDetailResponse(
                donation.Id, donation.DonorId, donation.Donor.Name, donation.Amount, donation.Currency, donation.FundType,
                donation.ReceivedDate, donation.PaymentMethod, donation.AdminAllowed, donation.AdminRate, donation.AmilRate,
                donation.Reference, donation.Notes, donation.IsVoided,
                donation.Allocation is null
                    ? null
                    : new DonationAllocationResponse(donation.Allocation.ProgramAmount, donation.Allocation.AdminAmount, donation.Allocation.AmilAmount));

            return Results.Ok(response);
        })
        .WithName("GetDonationById")
        .WithTags("Donations")
        .RequireAuthorization();
    }
}
