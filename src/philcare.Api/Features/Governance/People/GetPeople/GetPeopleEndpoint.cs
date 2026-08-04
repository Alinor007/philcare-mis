using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.People.GetPeople;

public sealed record PersonListItemResponse(int Id, string FullName, string PersonCategory, string Status, bool IsActive);

public sealed class GetPeopleEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/people", async (
            string? personCategory, string? status, bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.GovernancePeople.AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(personCategory))
            {
                query = query.Where(p => p.PersonCategory == personCategory);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var people = await query
                .OrderBy(p => p.FullName)
                .Select(p => new PersonListItemResponse(p.Id, p.FullName, p.PersonCategory, p.Status, p.IsActive))
                .ToListAsync(ct);

            return Results.Ok(people);
        })
        .WithName("GetPeople")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
