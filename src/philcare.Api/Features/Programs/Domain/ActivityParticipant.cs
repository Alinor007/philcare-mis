using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Domain;

/// <summary>Enrollment / attendance join between an Activity and a Participant.</summary>
public class ActivityParticipant : Entity
{
    public int ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;

    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;

    public string? RoleInActivity { get; set; }
    public string? AttendanceStatus { get; set; }
    public bool ConsentRequired { get; set; }
    public string? EvidenceLink { get; set; }
    public string? Remarks { get; set; }
}
