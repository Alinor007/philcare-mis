using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Distributions.GetDistributions;

public sealed record DistributionListItemResponse(
    int Id, string DistributionType, int BeneficiaryId, string BeneficiaryName, decimal TotalValuePhp, DateTime DistributionDate, bool IsVoided);

public sealed class GetDistributionsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/distributions", async (
            int? beneficiaryId,
            int? activityId,
            string? distributionType,
            DateTime? from,
            DateTime? to,
            bool? includeVoided,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var query = db.Distributions.Include(d => d.Beneficiary).AsQueryable();

            if (includeVoided != true)
            {
                query = query.Where(d => !d.IsVoided);
            }

            if (beneficiaryId is not null)
            {
                query = query.Where(d => d.BeneficiaryId == beneficiaryId);
            }

            if (activityId is not null)
            {
                query = query.Where(d => d.ActivityId == activityId);
            }

            if (!string.IsNullOrWhiteSpace(distributionType))
            {
                query = query.Where(d => d.DistributionType == distributionType);
            }

            if (from is not null)
            {
                query = query.Where(d => d.DistributionDate >= from);
            }

            if (to is not null)
            {
                query = query.Where(d => d.DistributionDate <= to);
            }

            var distributions = await query
                .OrderByDescending(d => d.DistributionDate)
                .Select(d => new DistributionListItemResponse(
                    d.Id, d.DistributionType, d.BeneficiaryId, d.Beneficiary.FullName, d.TotalValuePhp, d.DistributionDate, d.IsVoided))
                .ToListAsync(ct);

            return Results.Ok(distributions);
        })
        .WithName("GetDistributions")
        .WithTags("Distributions")
        .RequireAuthorization();
    }
}
