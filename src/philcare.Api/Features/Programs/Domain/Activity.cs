using philcare.Api.Common.Domain;
using philcare.Api.Features.Partners.Domain;
using philcare.Api.Features.HumanResources.Domain;

namespace philcare.Api.Features.Programs.Domain;

public class Activity : Entity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? ActivityCategory { get; set; }
    public string ActivityType { get; set; } = string.Empty; // lookup: activity_type
    public string? TargetGroup { get; set; }

    public string? Barangay { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Region { get; set; } // lookup: region

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Budget { get; set; }

    // Legacy free-text partner name — kept for one sprint alongside ImplementingPartnerId
    // (existing free text can't be reliably auto-matched to a Partner row); drop in Sprint 5.
    // Server-derived from ImplementingPartnerRef.Name whenever the FK is set (see Create/UpdateActivity
    // and UpdatePartnerHandler, which cascades a partner rename here) — never a second source of truth.
    public string? ImplementingPartner { get; set; }

    public int? ImplementingPartnerId { get; set; }
    public Partner? ImplementingPartnerRef { get; set; }

    public string? ResponsibleDepartment { get; set; }
    public string? SdgAlignment { get; set; }
    public string ImplementationStatus { get; set; } = "PLANNED"; // lookup: implementation_status
    public string? SafeguardingRisk { get; set; }
    public string? EvidenceLink { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Set when ImplementationStatus transitions to COMPLETED via ChangeActivityStatus — the
    // actual reach and finish date reported at closeout. Null until then; never set any other way.
    public int? ActualBeneficiaries { get; set; }
    public DateTime? ActualEndDate { get; set; }

    public List<ActivityParticipant> ActivityParticipants { get; set; } = [];
    public List<Distribution> Distributions { get; set; } = [];
    public List<ActivityVolunteer> ActivityVolunteers { get; set; } = [];
}
