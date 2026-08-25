using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.ReferenceData.Geography.GetCitiesMunicipalities;

public sealed record CityMunicipalityResponse(
    string Code, string Name, bool IsCity, bool IsCapital, string? ProvinceCode, string RegionCode);

/// <summary>
/// The 1,634 real PSGC cities/municipalities. Always filter by provinceCode OR regionCode in a
/// real form — the unfiltered list is the full national dataset, useful for search/admin tooling
/// but not a sane default for a location picker.
///
/// regionCode is the only way to reach NCR's 17 cities and the handful of other province-less
/// cities elsewhere (see CityMunicipality.ProvinceCode) — they have no provinceCode to filter by.
/// </summary>
public sealed class GetCitiesMunicipalitiesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cities", async (
            string? provinceCode, string? regionCode, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.CitiesMunicipalities.AsQueryable();

            if (!string.IsNullOrWhiteSpace(provinceCode))
            {
                query = query.Where(c => c.ProvinceCode == provinceCode);
            }
            else if (!string.IsNullOrWhiteSpace(regionCode))
            {
                query = query.Where(c => c.RegionCode == regionCode);
            }

            var cities = await query
                .OrderBy(c => c.Name)
                .Select(c => new CityMunicipalityResponse(c.Code, c.Name, c.IsCity, c.IsCapital, c.ProvinceCode, c.RegionCode))
                .ToListAsync(ct);

            return Results.Ok(cities);
        })
        .WithName("GetCitiesMunicipalities")
        .WithTags("Geography")
        .RequireAuthorization();
    }
}
