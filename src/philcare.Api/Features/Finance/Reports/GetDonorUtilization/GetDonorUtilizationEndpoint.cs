using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Reports.GetDonorUtilization;

public sealed record DonorUtilizationFundRow(
    string FundCode, int DonationCount, decimal TotalAmountPhp, decimal ProgramAmountPhp, decimal AdminAmountPhp);

public sealed record DonorUtilizationResponse(int DonorId, string DonorName, List<DonorUtilizationFundRow> Funds);

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
                    d.FundCode,
                    d.AmountPhp,
                    d.ProgramAllocationPhp,
                    d.AdminAllocationPhp
                })
                .ToListAsync(ct);

            var funds = donations
                .GroupBy(d => d.FundCode)
                .Select(g => new DonorUtilizationFundRow(
                    g.Key, g.Count(), g.Sum(d => d.AmountPhp), g.Sum(d => d.ProgramAllocationPhp), g.Sum(d => d.AdminAllocationPhp)))
                .OrderBy(r => r.FundCode)
                .ToList();

            return Results.Ok(new DonorUtilizationResponse(donor.Id, donor.Name, funds));
        })
        .WithName("GetDonorUtilization")
        .WithTags("Reports")
        .RequireAuthorization();
    }
}
