using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.Decisions.GetDecisionsByMinutes;

public sealed record DecisionListItemResponse(
    int Id, string DecisionText, string? ActionPoints, int? ResponsiblePersonId, string? ResponsiblePersonName,
    DateTime? DueDate, string DecisionStatus);

public sealed class GetDecisionsByMinutesEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/minutes/{minutesId:int}/decisions", async (int minutesId, AppDbContext db, CancellationToken ct) =>
        {
            var minutesExists = await db.MeetingMinutes.AnyAsync(mm => mm.Id == minutesId, ct);

            if (!minutesExists)
            {
                return Results.Problem(title: "Governance.MinutesNotFound", detail: "Minutes not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var decisions = await db.MeetingDecisions
                .Where(d => d.MeetingMinutesId == minutesId)
                .Include(d => d.ResponsiblePerson)
                .Select(d => new DecisionListItemResponse(
                    d.Id, d.DecisionText, d.ActionPoints, d.ResponsiblePersonId,
                    d.ResponsiblePerson == null ? null : d.ResponsiblePerson.FullName, d.DueDate, d.DecisionStatus))
                .ToListAsync(ct);

            return Results.Ok(decisions);
        })
        .WithName("GetDecisionsByMinutes")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
