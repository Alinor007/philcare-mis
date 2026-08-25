using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Zakat.Domain;

namespace philcare.Api.Features.Zakat.GetZakatEligibilities;

public sealed record ZakatEligibilityListItemResponse(
    int Id, int BeneficiaryId, string BeneficiaryName, string AsnafCategory, ZakatEligibilityStatus Status, DateTime? ValidUntil);

public sealed class GetZakatEligibilitiesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/zakat-eligibilities", async (
            int? beneficiaryId, ZakatEligibilityStatus? status, string? asnaf, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.ZakatEligibilities.Include(z => z.Beneficiary).AsQueryable();

            if (beneficiaryId is not null)
            {
                query = query.Where(z => z.BeneficiaryId == beneficiaryId);
            }

            if (status is not null)
            {
                query = query.Where(z => z.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(asnaf))
            {
                query = query.Where(z => z.AsnafCategory == asnaf);
            }

            var eligibilities = await query
                .OrderByDescending(z => z.AssessmentDate)
                .Select(z => new ZakatEligibilityListItemResponse(
                    z.Id, z.BeneficiaryId, z.Beneficiary.FullName, z.AsnafCategory, z.Status, z.ValidUntil))
                .ToListAsync(ct);

            return Results.Ok(eligibilities);
        })
        .WithName("GetZakatEligibilities")
        .WithTags("ZakatEligibility")
        .RequireAuthorization("Casework");
    }
}
