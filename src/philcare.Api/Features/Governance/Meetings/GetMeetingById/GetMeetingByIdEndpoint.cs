using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.Meetings.GetMeetingById;

public sealed record MeetingDetailResponse(
    int Id,
    int OrgBodyId,
    string OrgBodyName,
    string MeetingType,
    DateTime MeetingDate,
    string Mode,
    string? CalledBy,
    int? ChairPersonId,
    string? ChairPersonName,
    int? SecretaryPersonId,
    string? SecretaryPersonName,
    string? QuorumRequired,
    string? DecisionThreshold,
    string Status,
    DateTime? PublicationDeadline,
    string? Notes,
    int ParticipantCount,
    bool HasMinutes);

public sealed class GetMeetingByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/meetings/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var meeting = await db.Meetings
                .Where(m => m.Id == id)
                .Select(m => new MeetingDetailResponse(
                    m.Id, m.OrgBodyId, m.OrgBody.Name, m.MeetingType, m.MeetingDate, m.Mode, m.CalledBy,
                    m.ChairPersonId, m.ChairPerson == null ? null : m.ChairPerson.FullName,
                    m.SecretaryPersonId, m.SecretaryPerson == null ? null : m.SecretaryPerson.FullName,
                    m.QuorumRequired, m.DecisionThreshold, m.Status.ToString(), m.PublicationDeadline, m.Notes,
                    m.Participants.Count, m.Minutes != null))
                .FirstOrDefaultAsync(ct);

            if (meeting is null)
            {
                return Results.Problem(title: "Governance.MeetingNotFound", detail: "Meeting not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(meeting);
        })
        .WithName("GetMeetingById")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
