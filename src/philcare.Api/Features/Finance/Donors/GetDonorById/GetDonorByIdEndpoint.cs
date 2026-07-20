using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Donors.GetDonorById;

public sealed record DonorDetailResponse(
    int Id, string Name, DonorType Type, string? Email, string? Phone, string? Address, string? Notes, bool IsActive);

public sealed class GetDonorByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/donors/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var donor = await db.Donors
                .Where(d => d.Id == id)
                .Select(d => new DonorDetailResponse(d.Id, d.Name, d.Type, d.Email, d.Phone, d.Address, d.Notes, d.IsActive))
                .FirstOrDefaultAsync(ct);

            if (donor is null)
            {
                return Results.Problem(title: "Donors.NotFound", detail: "Donor not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(donor);
        })
        .WithName("GetDonorById")
        .WithTags("Donors")
        .RequireAuthorization();
    }
}
