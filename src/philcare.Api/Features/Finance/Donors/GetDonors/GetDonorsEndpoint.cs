using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Donors.GetDonors;

public sealed record DonorListItemResponse(int Id, string Name, DonorType Type, string? Email, string? Phone, bool IsActive);

public sealed class GetDonorsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/donors", async (bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Donors.AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(d => d.IsActive);
            }

            var donors = await query
                .OrderBy(d => d.Name)
                .Select(d => new DonorListItemResponse(d.Id, d.Name, d.Type, d.Email, d.Phone, d.IsActive))
                .ToListAsync(ct);

            return Results.Ok(donors);
        })
        .WithName("GetDonors")
        .WithTags("Donors")
        .RequireAuthorization();
    }
}
