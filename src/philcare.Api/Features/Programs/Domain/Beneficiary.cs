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

    // Optional profile photo — see StaffMember.PhotoUrl. This one is beneficiary PII: whatever
    // hosts the file needs the same Program/Admin gate the rest of this record has, or the photo
    // becomes the way around it.
    public string? PhotoUrl { get; set; }

    // No ActivityParticipants navigation: that join is the Activity *staffing* roster and belongs
    // to StaffMember. A beneficiary's link to an activity is the aid they received there.

    // Every distribution that reached this person, via the reach roster. There is no direct
    // Distribution collection: a distribution has no single recipient, so the only honest link is
    // through the roster.
    public List<DistributionBeneficiary> DistributionBeneficiaries { get; set; } = [];
}
