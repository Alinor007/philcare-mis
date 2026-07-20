using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.ReferenceData.GetLookups;

public sealed record LookupItemResponse(int Id, string Category, string Code, string Label, int SortOrder, bool IsActive);

public sealed class GetLookupsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/lookups", async (AppDbContext db, CancellationToken ct) =>
        {
            var items = await db.LookupItems
                .Where(l => l.IsActive)
                .OrderBy(l => l.Category).ThenBy(l => l.SortOrder)
                .Select(l => new LookupItemResponse(l.Id, l.Category, l.Code, l.Label, l.SortOrder, l.IsActive))
                .ToListAsync(ct);

            return Results.Ok(items);
        })
        .WithName("GetLookups")
        .WithTags("ReferenceData")
        .RequireAuthorization();

        app.MapGet("/api/lookups/{category}", async (string category, AppDbContext db, CancellationToken ct) =>
        {
            var items = await db.LookupItems
                .Where(l => l.IsActive && l.Category == category)
                .OrderBy(l => l.SortOrder)
                .Select(l => new LookupItemResponse(l.Id, l.Category, l.Code, l.Label, l.SortOrder, l.IsActive))
                .ToListAsync(ct);

            return Results.Ok(items);
        })
        .WithName("GetLookupsByCategory")
        .WithTags("ReferenceData")
        .RequireAuthorization();
    }
}
