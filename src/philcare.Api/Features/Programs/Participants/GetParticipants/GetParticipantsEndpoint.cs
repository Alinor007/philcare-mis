using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Participants.GetParticipants;

public sealed record ParticipantListItemResponse(
    int Id, string FullName, string ParticipantType, Gender Gender, string Status, bool IsActive);

public sealed class GetParticipantsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/participants", async (
            string? participantType,
            string? status,
            bool? includeInactive,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var query = db.Participants.AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(participantType))
            {
                query = query.Where(p => p.ParticipantType == participantType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var participants = await query
                .OrderBy(p => p.FullName)
                .Select(p => new ParticipantListItemResponse(p.Id, p.FullName, p.ParticipantType, p.Gender, p.Status, p.IsActive))
                .ToListAsync(ct);

            return Results.Ok(participants);
        })
        .WithName("GetParticipants")
        .WithTags("Participants")
        .RequireAuthorization();
    }
}
