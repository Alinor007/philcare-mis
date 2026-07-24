using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Participants.GetParticipantById;

public sealed record ParticipantDetailResponse(
    int Id,
    string FullName,
    string ParticipantType,
    Gender Gender,
    string? AgeGroup,
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
    bool IsActive);

public sealed class GetParticipantByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/participants/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var participant = await db.Participants
                .Where(p => p.Id == id)
                .Select(p => new ParticipantDetailResponse(
                    p.Id, p.FullName, p.ParticipantType, p.Gender, p.AgeGroup, p.Phone, p.Barangay, p.City, p.Province,
                    p.Region, p.Country, p.VulnerabilityCategory, p.SafeguardingCategory, p.ConsentOnFile, p.Status,
                    p.Remarks, p.IsActive))
                .FirstOrDefaultAsync(ct);

            if (participant is null)
            {
                return Results.Problem(title: "Participants.NotFound", detail: "Participant not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(participant);
        })
        .WithName("GetParticipantById")
        .WithTags("Participants")
        .RequireAuthorization();
    }
}
