using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Partners.GetPartners;

public sealed record PartnerListItemResponse(int Id, string Name, string PartnerType, string? ContactPerson, bool IsActive);

public sealed class GetPartnersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/partners", async (string? partnerType, bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Partners.AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(partnerType))
            {
                query = query.Where(p => p.PartnerType == partnerType);
            }

            var partners = await query
                .OrderBy(p => p.Name)
                .Select(p => new PartnerListItemResponse(p.Id, p.Name, p.PartnerType, p.ContactPerson, p.IsActive))
                .ToListAsync(ct);

            return Results.Ok(partners);
        })
        .WithName("GetPartners")
        .WithTags("Partners")
        .RequireAuthorization();
    }
}
