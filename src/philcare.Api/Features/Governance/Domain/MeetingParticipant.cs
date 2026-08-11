using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Governance.Domain;

/// <summary>Attendance/voting record for a Person at a Meeting.</summary>
public class MeetingParticipant : Entity
{
    public int MeetingId { get; set; }
    public Meeting Meeting { get; set; } = null!;

    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    // The Assignment that granted this person's voting right at this meeting, if any.
    public int? AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }

    public string? RoleInMeeting { get; set; } // lookup: meeting_role
    public string AttendanceStatus { get; set; } = string.Empty; // lookup: attendance_status
    public bool VotingRight { get; set; }
    public bool CountsForQuorum { get; set; }
    public string? ParticipationMode { get; set; } // lookup: participation_mode
    public string? Remarks { get; set; }
}
