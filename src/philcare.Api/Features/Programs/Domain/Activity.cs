using philcare.Api.Common.Domain;

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

    public string? ImplementingPartner { get; set; }
    public string? ResponsibleDepartment { get; set; }
    public string? SdgAlignment { get; set; }
    public string ImplementationStatus { get; set; } = "PLANNED"; // lookup: implementation_status
    public string? SafeguardingRisk { get; set; }
    public string? EvidenceLink { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<ActivityParticipant> ActivityParticipants { get; set; } = [];
    public List<Distribution> Distributions { get; set; } = [];
}
