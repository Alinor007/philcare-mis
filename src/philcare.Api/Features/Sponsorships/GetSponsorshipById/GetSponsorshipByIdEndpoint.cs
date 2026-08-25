using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Sponsorships.GetSponsorshipById;

public sealed record SponsorshipDetailResponse(
    int Id,
    int DonorId,
    string DonorName,
    int BeneficiaryId,
    string BeneficiaryName,
    string SponsorshipType,
    decimal MonthlyAmountPhp,
    DateTime StartDate,
    DateTime? EndDate,
    string Status,
    string? CaseWorker,
    DateTime? NextReviewDate,
    string? Notes);

public sealed class GetSponsorshipByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sponsorships/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var sponsorship = await db.Sponsorships
                .Where(s => s.Id == id)
                .Select(s => new SponsorshipDetailResponse(
                    s.Id, s.DonorId, s.Donor.Name, s.BeneficiaryId, s.Beneficiary.FullName, s.SponsorshipType, s.MonthlyAmountPhp,
                    s.StartDate, s.EndDate, s.Status.ToString(), s.CaseWorker, s.NextReviewDate, s.Notes))
                .FirstOrDefaultAsync(ct);

            if (sponsorship is null)
            {
                return Results.Problem(title: "Sponsorships.NotFound", detail: "Sponsorship not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(sponsorship);
        })
        .WithName("GetSponsorshipById")
        .WithTags("Sponsorships")
        .RequireAuthorization();
    }
}
