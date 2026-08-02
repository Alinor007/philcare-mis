using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Governance.Domain;

public enum MinutesStatus
{
    Draft,
    Submitted,
    Approved,
    Published,
    Returned
}

/// <summary>
/// The minutes record for a Meeting — exactly one per meeting. Decisions are a separate,
/// properly-keyed child entity (MeetingDecision) rather than living inline here, because the
/// source workbook's "Minutes ID" was reused across multiple decision rows for one meeting.
/// </summary>
public class MeetingMinutes : Entity
{
    public int MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;

    public int? PreparedByPersonId { get; set; }
    public Person? PreparedByPerson { get; set; }

    public int? ApprovedByPersonId { get; set; }
    public Person? ApprovedByPerson { get; set; }

    public string? Summary { get; set; }
    public DateTime? NextMeetingDate { get; set; }
    public string? DocumentLink { get; set; }
    public MinutesStatus PublicationStatus { get; set; } = MinutesStatus.Draft;

    public List<MeetingDecision> Decisions { get; set; } = [];
}
