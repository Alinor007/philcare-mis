using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Reports.GetBeneficiaryMasterList;

public sealed record BeneficiaryMasterListRow(
    int BeneficiaryId,
    string FullName,
    string BeneficiaryType,
    string? VulnerabilityCategory,
    string? SafeguardingCategory,
    string Status,
    bool ConsentOnFile,
    int ActivityCount,
    int DistributionCount,
    decimal TotalReceivedValuePhp);

/// <summary>
/// Workflow Phase 09 "Beneficiary Master List — vulnerability breakdown". PII-bearing (name,
/// vulnerability/safeguarding category, consent status) — same "Program Officer and Admin only"
/// boundary as GetBeneficiaries/GetBeneficiaryById, not the open-to-any-authenticated-role default
/// most other report endpoints use.
/// </summary>
public sealed class GetBeneficiaryMasterListEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/beneficiary-master-list", async (
            string? vulnerabilityCategory, bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Beneficiaries.AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(vulnerabilityCategory))
            {
                query = query.Where(p => p.VulnerabilityCategory == vulnerabilityCategory);
            }

            var people = await query
                .OrderBy(p => p.FullName)
                .Select(p => new
                {
                    p.Id, p.FullName, p.BeneficiaryType, p.VulnerabilityCategory,
                    p.SafeguardingCategory, p.Status, p.ConsentOnFile
                })
                .ToListAsync(ct);

            var personIds = people.Select(p => p.Id).ToList();

            // Reach comes from the roster, not from Distribution.BeneficiaryId — that FK only names
            // the primary recipient, so counting through it misses everyone else at the same event.
            // RecipientCount is carried so value can be apportioned below.
            var reach = await db.DistributionBeneficiaries
                .Where(r => personIds.Contains(r.BeneficiaryId) && r.IsActive && !r.Distribution.IsVoided)
                .Select(r => new
                {
                    r.BeneficiaryId,
                    r.Distribution.ActivityId,
                    r.Distribution.TotalValuePhp,
                    RecipientCount = r.Distribution.Beneficiaries.Count(x => x.IsActive)
                })
                .ToListAsync(ct);

            var reachByPerson = reach.GroupBy(r => r.BeneficiaryId).ToDictionary(g => g.Key, g => g.ToList());

            var rows = people
                .Select(p =>
                {
                    var received = reachByPerson.GetValueOrDefault(p.Id, []);

                    return new BeneficiaryMasterListRow(
                        p.Id, p.FullName, p.BeneficiaryType, p.VulnerabilityCategory,
                        p.SafeguardingCategory, p.Status, p.ConsentOnFile,
                        // Activities reached, derived from distributions: the activity roster now
                        // holds staff, so a beneficiary's link to an activity is the aid received
                        // there. Distributions with no activity are excluded, since they cannot be
                        // attributed to one.
                        received.Where(r => r.ActivityId != null).Select(r => r.ActivityId).Distinct().Count(),
                        received.Count,
                        // An even share of each event's total, NOT the whole total. A distribution's
                        // cost sits on the header and is expensed once; every recipient received the
                        // same handout, so one person's share is the event value divided by how many
                        // received it. Summing the full TotalValuePhp per roster row would multiply
                        // a single event's cost by its headcount across this report.
                        received.Sum(r => r.RecipientCount > 0
                            ? Math.Round(r.TotalValuePhp / r.RecipientCount, 2)
                            : 0m));
                })
                .ToList();

            return Results.Ok(rows);
        })
        .WithName("GetBeneficiaryMasterList")
        .WithTags("Reports")
        .RequireAuthorization("Program");
    }
}
