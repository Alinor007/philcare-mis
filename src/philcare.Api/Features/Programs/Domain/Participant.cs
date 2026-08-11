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
    public string FullName { get; set; } = string.Empty; //linked to Person.FullName or ProjectBeneficiary.FullName
    public string ParticipantType { get; set; } = string.Empty; // lookup: participant_type
    // Individual vs institution/organization — an institution (school, madrasah) or community
    // organization can itself be registered as an aid recipient, not just a natural person.
    public string BeneficiaryType { get; set; } = "INDIVIDUAL"; // lookup: beneficiary_type /can be removed
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

    // ActivityParticipants moved to StaffMember — that join is now the Activity staffing roster.
    public List<Distribution> Distributions { get; set; } = [];
}
