using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.People.Memberships.GetMembershipById;

public sealed record MembershipDetailResponse(
    int Id, int PersonId, string FullName, string? Email, string? ContactNumber,
    string MembershipNumber, string MembershipType, string Status,
    DateTime? JoinDate, DateTime? RenewalDate, DateTime? ExitDate,
    string? ReferredBy, string? Notes, bool IsActive);

public sealed class GetMembershipByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/memberships/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var membership = await db.Memberships
                .Where(m => m.Id == id)
                .Select(m => new MembershipDetailResponse(
                    m.Id, m.PersonId, m.Person.FullName, m.Person.Email, m.Person.ContactNumber,
                    m.MembershipNumber, m.MembershipType, m.Status,
                    m.JoinDate, m.RenewalDate, m.ExitDate, m.ReferredBy, m.Notes, m.IsActive))
                .FirstOrDefaultAsync(ct);

            if (membership is null)
            {
                return Results.Problem(title: "Memberships.NotFound", detail: "Membership not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(membership);
        })
        .WithName("GetMembershipById")
        .WithTags("Memberships")
        .RequireAuthorization();
    }
}
