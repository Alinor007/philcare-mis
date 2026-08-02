using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.DonorEngagements.GetDonorEngagements;

public sealed record DonorEngagementListItemResponse(
    int Id, int DonorId, string DonorName, string EngagementType, DateTime EngagementDate,
    string Subject, string? Notes, bool FollowUpRequired, DateTime? FollowUpDate,
    string? CreatedBy, DateTime CreatedAt);

public sealed class GetDonorEngagementsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/donor-engagements", async (
            int? donorId,
            string? engagementType,
            bool? followUpRequired,
            DateTime? from,
            DateTime? to,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var query = db.DonorEngagements.Include(e => e.Donor).AsQueryable();

            if (donorId is not null)
            {
                query = query.Where(e => e.DonorId == donorId);
            }

            if (!string.IsNullOrWhiteSpace(engagementType))
            {
                query = query.Where(e => e.EngagementType == engagementType);
            }

            if (followUpRequired is not null)
            {
                query = query.Where(e => e.FollowUpRequired == followUpRequired);
            }

            if (from is not null)
            {
                query = query.Where(e => e.EngagementDate >= from);
            }

            if (to is not null)
            {
                query = query.Where(e => e.EngagementDate <= to);
            }

            var engagements = await query
                .OrderByDescending(e => e.EngagementDate)
                .ThenByDescending(e => e.Id)
                .Select(e => new DonorEngagementListItemResponse(
                    e.Id, e.DonorId, e.Donor.Name, e.EngagementType, e.EngagementDate,
                    e.Subject, e.Notes, e.FollowUpRequired, e.FollowUpDate, e.CreatedBy, e.CreatedAt))
                .ToListAsync(ct);

            return Results.Ok(engagements);
        })
        .WithName("GetDonorEngagements")
        .WithTags("DonorEngagements")
        .RequireAuthorization();
    }
}
