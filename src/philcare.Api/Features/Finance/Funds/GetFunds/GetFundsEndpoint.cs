using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Funds.GetFunds;

public sealed record FundResponse(
    int Id, string Code, string Name, bool IsRestricted, string? PolicyNotes, string? UseCase, bool SeparateTrackingRequired);

public sealed class GetFundsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/funds", async (AppDbContext db, CancellationToken ct) =>
        {
            var funds = await db.Funds
                .OrderBy(f => f.Code)
                .Select(f => new FundResponse(f.Id, f.Code, f.Name, f.IsRestricted, f.PolicyNotes, f.UseCase, f.SeparateTrackingRequired))
                .ToListAsync(ct);

            return Results.Ok(funds);
        })
        .WithName("GetFunds")
        .WithTags("Funds")
        .RequireAuthorization();
    }
}
