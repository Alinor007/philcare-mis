using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.People.Memberships.GetMemberships;

public sealed record MembershipListItemResponse(
    int Id, int PersonId, string FullName, string MembershipNumber, string MembershipType,
    string Status, DateTime? JoinDate, DateTime? RenewalDate, bool IsActive);

public sealed class GetMembershipsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/memberships", async (
            int? personId,
            string? membershipType,
            string? status,
            string? search,
            bool? includeInactive,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var query = db.Memberships.AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(m => m.IsActive);
            }

            if (personId is not null)
            {
                query = query.Where(m => m.PersonId == personId);
            }

            if (!string.IsNullOrWhiteSpace(membershipType))
            {
                query = query.Where(m => m.MembershipType == membershipType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(m => m.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                // Name comes from Person; the number is the roll's own identifier.
                query = query.Where(m => m.Person.FullName.Contains(search) || m.MembershipNumber.Contains(search));
            }

            var memberships = await query
                .OrderBy(m => m.Person.FullName)
                .Select(m => new MembershipListItemResponse(
                    m.Id, m.PersonId, m.Person.FullName, m.MembershipNumber, m.MembershipType,
                    m.Status, m.JoinDate, m.RenewalDate, m.IsActive))
                .ToListAsync(ct);

            return Results.Ok(memberships);
        })
        .WithName("GetMemberships")
        .WithTags("Memberships")
        .RequireAuthorization();
    }
}
