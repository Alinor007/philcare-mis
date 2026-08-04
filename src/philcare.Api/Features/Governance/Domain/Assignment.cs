using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Governance.Domain;

public enum AssignmentStatus
{
    Current,
    Former
}

/// <summary>
/// Temporal bridge: a Person holding a GovernanceRole in an OrgBody for a period of time.
/// This is the entity "board trustees" and "executive team" rosters are derived from —
/// there are no separate BoardTrustee/ExecutiveTeamMember tables (see GetOrgBodyMembers).
/// </summary>
public class Assignment : Entity
{
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public int OrgBodyId { get; set; }
    public OrgBody OrgBody { get; set; } = null!;

    public int GovernanceRoleId { get; set; }
    public GovernanceRole GovernanceRole { get; set; } = null!;

    public string? PositionTitle { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsPrimary { get; set; }
    public bool VotingRights { get; set; }
    public bool IsTemporary { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Current;
    public string? Notes { get; set; }

    /// <summary>
    /// true only while this is the person's primary Current assignment; null otherwise.
    /// Backed by a unique index on (PersonId, IsPrimaryCurrent) — the same MariaDB 10.4
    /// filtered-unique-index substitute used by ZakatEligibility.IsLiveApproval.
    /// </summary>
    public bool? IsPrimaryCurrent { get; set; }
}
