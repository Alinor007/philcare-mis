using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Reports.GetBeneficiaryMasterList;

public sealed record BeneficiaryMasterListRow(
    int ParticipantId,
    string FullName,
    string ParticipantType,
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
/// boundary as GetParticipants/GetParticipantById, not the open-to-any-authenticated-role default
/// most other report endpoints use.
/// </summary>
public sealed class GetBeneficiaryMasterListEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/beneficiary-master-list", async (
            string? vulnerabilityCategory, bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Participants.AsQueryable();

            if (includeInactive != true)
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(vulnerabilityCategory))
            {
                query = query.Where(p => p.VulnerabilityCategory == vulnerabilityCategory);
            }

            var rows = await query
                .OrderBy(p => p.FullName)
                .Select(p => new BeneficiaryMasterListRow(
                    p.Id,
                    p.FullName,
                    p.ParticipantType,
                    p.BeneficiaryType,
                    p.VulnerabilityCategory,
                    p.SafeguardingCategory,
                    p.Status,
                    p.ConsentOnFile,
                    // Activities reached, derived from distributions: the activity roster now
                    // holds staff, so a beneficiary's link to an activity is the aid received
                    // there. Distributions with no activity are excluded, since they cannot be
                    // attributed to one.
                    p.Distributions.Where(d => !d.IsVoided && d.ActivityId != null)
                        .Select(d => d.ActivityId).Distinct().Count(),
                    p.Distributions.Count(d => !d.IsVoided),
                    p.Distributions.Where(d => !d.IsVoided).Sum(d => d.TotalValuePhp)))
                .ToListAsync(ct);

            return Results.Ok(rows);
        })
        .WithName("GetBeneficiaryMasterList")
        .WithTags("Reports")
        .RequireAuthorization("Program");
    }
}
