using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.DistributionBeneficiaries.GetDistributionBeneficiaries;

/// <summary>A reach-roster row: who received aid at this distribution, and whether they signed for it.</summary>
public sealed record DistributionRosterRow(
    int BeneficiaryId, string BeneficiaryName, string BeneficiaryType,
    bool ReceivedConfirmation, string? EvidenceLink, string? Remarks, bool IsActive);

public sealed class GetDistributionBeneficiariesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/distributions/{distributionId:int}/beneficiaries", async (
            int distributionId, bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var distributionExists = await db.Distributions.AnyAsync(d => d.Id == distributionId, ct);

            if (!distributionExists)
            {
                return Results.Problem(
                    title: "Distributions.NotFound", detail: "Distribution not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var query = db.DistributionBeneficiaries.Where(r => r.DistributionId == distributionId);

            if (includeInactive != true)
            {
                query = query.Where(r => r.IsActive);
            }

            // Plain alphabetical: every recipient is equal now, so there is no head of the list.
            var roster = await query
                .Include(r => r.Beneficiary)
                .OrderBy(r => r.Beneficiary.FullName)
                .Select(r => new DistributionRosterRow(
                    r.BeneficiaryId, r.Beneficiary.FullName, r.Beneficiary.BeneficiaryType,
                    r.ReceivedConfirmation, r.EvidenceLink, r.Remarks, r.IsActive))
                .ToListAsync(ct);

            return Results.Ok(roster);
        })
        .WithName("GetDistributionBeneficiaries")
        .WithTags("DistributionBeneficiaries")
        .RequireAuthorization();
    }
}
