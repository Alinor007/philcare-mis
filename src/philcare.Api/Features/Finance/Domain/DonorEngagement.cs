using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

/// <summary>
/// A logged touchpoint with a donor — a call, meeting, KYD-review contact, thank-you, etc.
/// Staff attribution comes from the audit fields on <see cref="Entity"/> (CreatedBy/CreatedAt),
/// not a dedicated staff reference, so there is no separate "logged by" field here.
/// </summary>
public class DonorEngagement : Entity
{
    public int DonorId { get; set; }
    public Donor Donor { get; set; } = null!;

    public string EngagementType { get; set; } = string.Empty; // lookup: engagement_type
    public DateTime EngagementDate { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public bool FollowUpRequired { get; set; }
    public DateTime? FollowUpDate { get; set; }
}
