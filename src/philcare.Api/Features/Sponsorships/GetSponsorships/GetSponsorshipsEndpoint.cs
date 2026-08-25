using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Sponsorships.Domain;

namespace philcare.Api.Features.Sponsorships.GetSponsorships;

public sealed record SponsorshipListItemResponse(
    int Id, int DonorId, string DonorName, int BeneficiaryId, string BeneficiaryName,
    string SponsorshipType, decimal MonthlyAmountPhp, SponsorshipStatus Status);

public sealed class GetSponsorshipsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sponsorships", async (
            int? donorId,
            int? beneficiaryId,
            SponsorshipStatus? status,
            string? sponsorshipType,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var query = db.Sponsorships
                .Include(s => s.Donor)
                .Include(s => s.Beneficiary)
                .AsQueryable();

            if (donorId is not null)
            {
                query = query.Where(s => s.DonorId == donorId);
            }

            if (beneficiaryId is not null)
            {
                query = query.Where(s => s.BeneficiaryId == beneficiaryId);
            }

            if (status is not null)
            {
                query = query.Where(s => s.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(sponsorshipType))
            {
                query = query.Where(s => s.SponsorshipType == sponsorshipType);
            }

            var sponsorships = await query
                .OrderBy(s => s.Beneficiary.FullName)
                .Select(s => new SponsorshipListItemResponse(
                    s.Id, s.DonorId, s.Donor.Name, s.BeneficiaryId, s.Beneficiary.FullName, s.SponsorshipType, s.MonthlyAmountPhp, s.Status))
                .ToListAsync(ct);

            return Results.Ok(sponsorships);
        })
        .WithName("GetSponsorships")
        .WithTags("Sponsorships")
        .RequireAuthorization();
    }
}
