using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Common.Domain;

namespace philcare.Api.Features.HumanResources.Volunteers.GetVolunteers;

public sealed record VolunteerListItemResponse(
    int Id, int PersonId, string FullName, Gender Gender, string Status, bool OrientationCompleted, bool IsActive);

public sealed class GetVolunteersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/volunteers", async (
            string? status, bool? orientationCompleted, bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Volunteers.AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(v => v.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(v => v.Status == status);
            }

            if (orientationCompleted is not null)
            {
                query = query.Where(v => v.OrientationCompleted == orientationCompleted);
            }

            var volunteers = await query
                .OrderBy(v => v.Person.FullName)
                .Select(v => new VolunteerListItemResponse(
                    v.Id, v.PersonId, v.Person.FullName, v.Person.Gender, v.Status, v.OrientationCompleted, v.IsActive))
                .ToListAsync(ct);

            return Results.Ok(volunteers);
        })
        .WithName("GetVolunteers")
        .WithTags("Volunteers")
        .RequireAuthorization();
    }
}
