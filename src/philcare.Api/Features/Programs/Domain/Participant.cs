using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Domain;

public enum Gender
{
    Male,
    Female,
    Unspecified
}

public class Participant : Entity
{
    public string FullName { get; set; } = string.Empty;
    public string ParticipantType { get; set; } = string.Empty; // lookup: participant_type
    public Gender Gender { get; set; } = Gender.Unspecified;
    public string? AgeGroup { get; set; } // lookup: age_group
    public string? Phone { get; set; }

    public string? Barangay { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }

    public string? VulnerabilityCategory { get; set; } // lookup: vulnerability_category
    public string? SafeguardingCategory { get; set; } // lookup: safeguarding_category
    public bool ConsentOnFile { get; set; }
    public string Status { get; set; } = "PENDING"; // lookup: beneficiary_status
    public string? Remarks { get; set; }
    public bool IsActive { get; set; } = true;

    public List<ActivityParticipant> ActivityParticipants { get; set; } = [];
    public List<Distribution> Distributions { get; set; } = [];
}
