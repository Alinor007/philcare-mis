using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Distributions.GetDistributionById;

public sealed record DistributionDetailResponse(
    int Id,
    string DistributionType,
    int ParticipantId,
    string ParticipantName,
    int? ActivityId,
    string? FundingBucketCode,
    int Quantity,
    decimal TotalValuePhp,
    DateTime DistributionDate,
    string? Location,
    bool FieldVerified,
    bool ReceivedConfirmation,
    string? ProcessedBy,
    string? ZakatAsnaf,
    string? Notes,
    bool IsVoided);

public sealed class GetDistributionByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/distributions/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var distribution = await db.Distributions
                .Where(d => d.Id == id)
                .Select(d => new DistributionDetailResponse(
                    d.Id, d.DistributionType, d.ParticipantId, d.Participant.FullName, d.ActivityId, d.FundingBucketCode,
                    d.Quantity, d.TotalValuePhp, d.DistributionDate, d.Location, d.FieldVerified, d.ReceivedConfirmation,
                    d.ProcessedBy, d.ZakatAsnaf, d.Notes, d.IsVoided))
                .FirstOrDefaultAsync(ct);

            if (distribution is null)
            {
                return Results.Problem(title: "Distributions.NotFound", detail: "Distribution not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(distribution);
        })
        .WithName("GetDistributionById")
        .WithTags("Distributions")
        .RequireAuthorization();
    }
}
