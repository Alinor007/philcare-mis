using philcare.Api.Common.Domain;
using philcare.Api.Features.People.Domain;

namespace philcare.Api.Features.Governance.Domain;

/// <summary>A single decision recorded in a meeting's minutes, with an optional action owner and due date.</summary>
public class MeetingDecision : Entity
{
    public int MeetingMinutesId { get; set; }
    public MeetingMinutes MeetingMinutes { get; set; } = null!;

    public string DecisionText { get; set; } = string.Empty;
    public string? ActionPoints { get; set; }

    public int? ResponsiblePersonId { get; set; }
    public Person? ResponsiblePerson { get; set; }

    public DateTime? DueDate { get; set; }
    public string DecisionStatus { get; set; } = string.Empty; // lookup: decision_status
    public string? Notes { get; set; }
}
