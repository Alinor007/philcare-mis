using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Zakat.GetZakatEligibilityById;

public sealed record ZakatEligibilityDetailResponse(
    int Id,
    int ParticipantId,
    string ParticipantName,
    string AsnafCategory,
    decimal? MonthlyIncomePhp,
    int? HouseholdSize,
    DateTime AssessmentDate,
    string? AssessedBy,
    string? AssessmentNotes,
    string Status,
    DateTime? DecisionDate,
    string? DecidedBy,
    DateTime? ValidUntil,
    string? RejectionReason,
    string? Notes);

public sealed class GetZakatEligibilityByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/zakat-eligibilities/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var eligibility = await db.ZakatEligibilities
                .Where(z => z.Id == id)
                .Select(z => new ZakatEligibilityDetailResponse(
                    z.Id, z.ParticipantId, z.Participant.FullName, z.AsnafCategory, z.MonthlyIncomePhp, z.HouseholdSize,
                    z.AssessmentDate, z.AssessedBy, z.AssessmentNotes, z.Status.ToString(), z.DecisionDate, z.DecidedBy,
                    z.ValidUntil, z.RejectionReason, z.Notes))
                .FirstOrDefaultAsync(ct);

            if (eligibility is null)
            {
                return Results.Problem(title: "Zakat.NotFound", detail: "Zakat eligibility case not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(eligibility);
        })
        .WithName("GetZakatEligibilityById")
        .WithTags("ZakatEligibility")
        .RequireAuthorization("Casework");
    }
}
