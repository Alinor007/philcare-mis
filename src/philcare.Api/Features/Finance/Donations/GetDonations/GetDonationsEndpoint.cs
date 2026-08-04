using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Donations.GetDonations;

public sealed record DonationListItemResponse(
    int Id, int DonorId, string DonorName, decimal AmountPhp, string FundCode, DateTime DateReceived, bool IsVoided,
    string? ReceiptNo, string? Purpose, string Channel);

public sealed class GetDonationsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/donations", async (
            int? donorId,
            string? fundCode,
            DateTime? from,
            DateTime? to,
            bool? includeVoided,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var query = db.Donations.Include(d => d.Donor).AsQueryable();

            if (includeVoided != true)
            {
                query = query.Where(d => !d.IsVoided);
            }

            if (donorId is not null)
            {
                query = query.Where(d => d.DonorId == donorId);
            }

            if (!string.IsNullOrWhiteSpace(fundCode))
            {
                query = query.Where(d => d.FundCode == fundCode);
            }

            if (from is not null)
            {
                query = query.Where(d => d.DateReceived >= from);
            }

            if (to is not null)
            {
                query = query.Where(d => d.DateReceived <= to);
            }

            var donations = await query
                .OrderByDescending(d => d.DateReceived)
                .Select(d => new DonationListItemResponse(
                    d.Id, d.DonorId, d.Donor.Name, d.AmountPhp, d.FundCode, d.DateReceived, d.IsVoided,
                    d.ReceiptNo, d.Purpose, d.Channel))
                .ToListAsync(ct);

            return Results.Ok(donations);
        })
        .WithName("GetDonations")
        .WithTags("Donations")
        .RequireAuthorization();
    }
}
