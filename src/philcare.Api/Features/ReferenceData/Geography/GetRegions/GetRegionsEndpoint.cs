using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.ReferenceData.Geography.GetRegions;

public sealed record RegionResponse(string Code, string Name, string DesignationName, string IslandGroup);

/// <summary>All 17 real PSGC regions — the top of the Region → Province → City/Municipality hierarchy.</summary>
public sealed class GetRegionsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/regions", async (AppDbContext db, CancellationToken ct) =>
        {
            var regions = await db.Regions
                .OrderBy(r => r.Code)
                .Select(r => new RegionResponse(r.Code, r.Name, r.DesignationName, r.IslandGroup))
                .ToListAsync(ct);

            return Results.Ok(regions);
        })
        .WithName("GetRegions")
        .WithTags("Geography")
        .RequireAuthorization();
    }
}
