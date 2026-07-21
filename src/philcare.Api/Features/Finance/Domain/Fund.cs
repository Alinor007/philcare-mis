using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

/// <summary>
/// Top-level fund master (e.g. Zakat, Sadaqah, General, Restricted, Operations, Capital).
/// Each fund splits into one or more <see cref="FundingBucket"/>s.
/// </summary>
public class Fund : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsRestricted { get; set; }
    public string? PolicyNotes { get; set; }
    public string? UseCase { get; set; }
    public bool SeparateTrackingRequired { get; set; }

    public List<FundingBucket> Buckets { get; set; } = [];
}
