using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Beneficiaries.GetBeneficiaries;

public sealed record BeneficiaryListItemResponse(
    int Id, string FullName, string BeneficiaryType, Gender Gender, string Status, bool IsActive);

public sealed class GetBeneficiariesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/beneficiaries", async (
            string? beneficiaryType,
            string? status,
            bool? includeInactive,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var query = db.Beneficiaries.AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(beneficiaryType))
            {
                query = query.Where(p => p.BeneficiaryType == beneficiaryType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var beneficiaries = await query
                .OrderBy(p => p.FullName)
                .Select(p => new BeneficiaryListItemResponse(p.Id, p.FullName, p.BeneficiaryType, p.Gender, p.Status, p.IsActive))
                .ToListAsync(ct);

            return Results.Ok(beneficiaries);
        })
        .WithName("GetBeneficiaries")
        .WithTags("Beneficiaries")
        .RequireAuthorization("Casework");
    }
}
