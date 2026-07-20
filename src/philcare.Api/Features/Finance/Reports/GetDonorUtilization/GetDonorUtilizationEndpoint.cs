using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Reports.GetDonorUtilization;

public sealed record DonorUtilizationFundTypeRow(
    string FundType, int DonationCount, decimal TotalAmount, decimal ProgramAmount, decimal AdminAmount, decimal AmilAmount);

public sealed record DonorUtilizationResponse(int DonorId, string DonorName, List<DonorUtilizationFundTypeRow> FundTypes);

public sealed class GetDonorUtilizationEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/donor-utilization/{donorId:int}", async (int donorId, AppDbContext db, CancellationToken ct) =>
        {
            var donor = await db.Donors.FirstOrDefaultAsync(d => d.Id == donorId, ct);

            if (donor is null)
            {
                return Results.Problem(title: "Donors.NotFound", detail: "Donor not found.", statusCode: StatusCodes.Status404NotFound);
            }

            var donations = await db.Donations
                .Where(d => d.DonorId == donorId && !d.IsVoided)
                .Select(d => new
                {
                    d.FundType,
                    d.Amount,
                    ProgramAmount = d.Allocation != null ? d.Allocation.ProgramAmount : 0,
                    AdminAmount = d.Allocation != null ? d.Allocation.AdminAmount : 0,
                    AmilAmount = d.Allocation != null ? d.Allocation.AmilAmount : 0
                })
                .ToListAsync(ct);

            var fundTypes = donations
                .GroupBy(d => d.FundType)
                .Select(g => new DonorUtilizationFundTypeRow(
                    g.Key, g.Count(), g.Sum(d => d.Amount), g.Sum(d => d.ProgramAmount), g.Sum(d => d.AdminAmount), g.Sum(d => d.AmilAmount)))
                .OrderBy(r => r.FundType)
                .ToList();

            return Results.Ok(new DonorUtilizationResponse(donor.Id, donor.Name, fundTypes));
        })
        .WithName("GetDonorUtilization")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
