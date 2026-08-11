using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Governance.Domain;

public enum MeetingStatus
{
    Scheduled,
    Held,
    Cancelled,
    Postponed
}

/// <summary>
/// A governance meeting of an OrgBody. QuorumRequired/DecisionThreshold are snapshot-copied
/// from OrgBody at creation time, not live-read from it, so a later change to the body's
/// policy does not silently rewrite the historical record of what applied to this meeting.
/// </summary>
public class Meeting : Entity
{
    public int OrgBodyId { get; set; }
    public OrgBody OrgBody { get; set; } = null!;

    public string MeetingType { get; set; } = string.Empty; // lookup: meeting_type
    public DateTime MeetingDate { get; set; }
    public string Mode { get; set; } = string.Empty; // lookup: meeting_mode
    public string? CalledBy { get; set; }

    public int? ChairPersonId { get; set; }
    public Person? ChairPerson { get; set; }

    public int? SecretaryPersonId { get; set; }
    public Person? SecretaryPerson { get; set; }

    public string? QuorumRequired { get; set; }
    public string? DecisionThreshold { get; set; }
    public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;
    public DateTime? PublicationDeadline { get; set; }
    public string? Notes { get; set; }

    public List<MeetingParticipant> Participants { get; set; } = [];
    public MeetingMinutes? Minutes { get; set; }
}
