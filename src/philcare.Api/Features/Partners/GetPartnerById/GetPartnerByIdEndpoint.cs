using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Partners.GetPartnerById;

public sealed record PartnerDetailResponse(
    int Id,
    string Name,
    string PartnerType,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    string? City,
    string? Province,
    string? Region,
    string? MouReference,
    DateTime? MouStartDate,
    DateTime? MouEndDate,
    string? AccreditationNotes,
    string? Notes,
    bool IsActive,
    int ActivityCount);

public sealed class GetPartnerByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/partners/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var partner = await db.Partners
                .Where(p => p.Id == id)
                .Select(p => new PartnerDetailResponse(
                    p.Id, p.Name, p.PartnerType, p.ContactPerson, p.Email, p.Phone, p.Address, p.City, p.Province, p.Region,
                    p.MouReference, p.MouStartDate, p.MouEndDate, p.AccreditationNotes, p.Notes, p.IsActive, p.Activities.Count))
                .FirstOrDefaultAsync(ct);

            if (partner is null)
            {
                return Results.Problem(title: "Partners.NotFound", detail: "Partner not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(partner);
        })
        .WithName("GetPartnerById")
        .WithTags("Partners")
        .RequireAuthorization();
    }
}
