using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.DistributionBeneficiaries.RemoveDistributionBeneficiary;

public sealed class RemoveDistributionBeneficiaryEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/distributions/{distributionId:int}/beneficiaries/{beneficiaryId:int}", async (
            int distributionId, int beneficiaryId, AppDbContext db, CancellationToken ct) =>
        {
            // Roster and expense loaded because DistributionReach.Sync writes to both.
            var distribution = await db.Distributions
                .Include(d => d.Beneficiaries)
                .Include(d => d.Expense)
                .FirstOrDefaultAsync(d => d.Id == distributionId, ct);

            var row = distribution?.Beneficiaries
                .FirstOrDefault(r => r.BeneficiaryId == beneficiaryId && r.IsActive);

            if (distribution is null || row is null)
            {
                return Results.Problem(
                    title: "DistributionBeneficiaries.NotFound",
                    detail: "This beneficiary is not on this distribution's roster.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            // Every row is an equal recipient and all of them are removable — there is no primary
            // to protect any more. Removing the last one just leaves the event at zero reach, which
            // is the same state it is created in.
            //
            // Soft delete — preserves the receipt/audit history. Re-adding reactivates this same
            // row (see AddDistributionBeneficiaryHandler); the unique index forbids a second insert.
            row.IsActive = false;

            DistributionReach.Sync(distribution);

            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("RemoveDistributionBeneficiary")
        .WithTags("DistributionBeneficiaries")
        .RequireAuthorization("Program");
    }
}
