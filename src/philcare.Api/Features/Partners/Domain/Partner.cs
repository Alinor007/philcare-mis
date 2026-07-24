using philcare.Api.Common.Domain;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Partners.Domain;

/// <summary>
/// Implementing/donor partner organization. Activities can link to a Partner via
/// ImplementingPartnerId; the legacy free-text Activity.ImplementingPartner string is kept
/// alongside it for one sprint (no reliable auto-match to backfill existing free-text values).
/// </summary>
public class Partner : Entity
{
    public string Name { get; set; } = string.Empty;
    public string PartnerType { get; set; } = string.Empty; // lookup: partner_type
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Region { get; set; }
    public string? MouReference { get; set; }
    public DateTime? MouStartDate { get; set; }
    public DateTime? MouEndDate { get; set; }
    public string? AccreditationNotes { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Activity> Activities { get; set; } = [];
}
