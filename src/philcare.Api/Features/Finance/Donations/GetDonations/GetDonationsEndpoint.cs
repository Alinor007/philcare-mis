using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Donations.GetDonations;

public sealed record DonationListItemResponse(
    int Id, int DonorId, string DonorName, decimal Amount, string FundType, DateTime ReceivedDate, bool IsVoided);

public sealed class GetDonationsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/donations", async (
            int? donorId,
            string? fundType,
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

            if (!string.IsNullOrWhiteSpace(fundType))
            {
                query = query.Where(d => d.FundType == fundType);
            }

            if (from is not null)
            {
                query = query.Where(d => d.ReceivedDate >= from);
            }

            if (to is not null)
            {
                query = query.Where(d => d.ReceivedDate <= to);
            }

            var donations = await query
                .OrderByDescending(d => d.ReceivedDate)
                .Select(d => new DonationListItemResponse(d.Id, d.DonorId, d.Donor.Name, d.Amount, d.FundType, d.ReceivedDate, d.IsVoided))
                .ToListAsync(ct);

            return Results.Ok(donations);
        })
        .WithName("GetDonations")
        .WithTags("Donations")
        .RequireAuthorization();
    }
}
