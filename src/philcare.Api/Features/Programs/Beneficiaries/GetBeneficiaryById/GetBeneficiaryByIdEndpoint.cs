using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Beneficiaries.GetBeneficiaryById;

public sealed record BeneficiaryDetailResponse(
    int Id,
    string FullName,
    string BeneficiaryType,
    Gender Gender,
    string? Phone,
    string? Barangay,
    string? City,
    string? Province,
    string? Region,
    string? Country,
    string? VulnerabilityCategory,
    string? SafeguardingCategory,
    bool ConsentOnFile,
    string Status,
    string? Remarks,
    string? PhotoUrl,
    bool IsActive);

public sealed class GetBeneficiaryByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/beneficiaries/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var beneficiary = await db.Beneficiaries
                .Where(p => p.Id == id)
                .Select(p => new BeneficiaryDetailResponse(
                    p.Id, p.FullName, p.BeneficiaryType, p.Gender, p.Phone, p.Barangay, p.City, p.Province,
                    p.Region, p.Country, p.VulnerabilityCategory, p.SafeguardingCategory, p.ConsentOnFile, p.Status,
                    p.Remarks, p.PhotoUrl, p.IsActive))
                .FirstOrDefaultAsync(ct);

            if (beneficiary is null)
            {
                return Results.Problem(title: "Beneficiaries.NotFound", detail: "Beneficiary not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(beneficiary);
        })
        .WithName("GetBeneficiaryById")
        .WithTags("Beneficiaries")
        .RequireAuthorization("Casework");
    }
}
