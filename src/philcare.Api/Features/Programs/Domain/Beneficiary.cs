using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Programs.Domain;


public class Beneficiary : Entity
{
    public string FullName { get; set; } = string.Empty; 
    public string BeneficiaryType { get; set; } = "INDIVIDUAL"; // lookup: beneficiary_type /can be removed
    public Gender Gender { get; set; } = Gender.Unspecified;
    public string? DateOfBirth { get; set; }

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

    // No ActivityParticipants navigation: that join is the Activity *staffing* roster and belongs
    // to StaffMember. A beneficiary's link to an activity is the aid they received there.
    public List<Distribution> Distributions { get; set; } = [];
}
