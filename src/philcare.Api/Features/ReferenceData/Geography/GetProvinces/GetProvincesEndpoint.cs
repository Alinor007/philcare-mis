using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.ReferenceData.Geography.GetProvinces;

public sealed record ProvinceResponse(string Code, string Name, string RegionCode);

/// <summary>
/// The 81 real PSGC provinces, optionally filtered by region — NCR has none (query it and get an
/// empty list, not an error; its cities sit directly under the region, see GetCitiesMunicipalities).
/// </summary>
public sealed class GetProvincesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/provinces", async (string? regionCode, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Provinces.AsQueryable();

            if (!string.IsNullOrWhiteSpace(regionCode))
            {
                query = query.Where(p => p.RegionCode == regionCode);
            }

            var provinces = await query
                .OrderBy(p => p.Name)
                .Select(p => new ProvinceResponse(p.Code, p.Name, p.RegionCode))
                .ToListAsync(ct);

            return Results.Ok(provinces);
        })
        .WithName("GetProvinces")
        .WithTags("Geography")
        .RequireAuthorization();
    }
}
