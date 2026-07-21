using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

public enum DonorType
{
    Individual,
    Organization,
    Partner
}

public class Donor : Entity
{
    public string Name { get; set; } = string.Empty;
    public DonorType Type { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Know-Your-Donor / AML compliance
    public KydStatus KydStatus { get; set; } = KydStatus.Pending;
    public RiskRating RiskRating { get; set; } = RiskRating.Low;
    public bool PepFlag { get; set; }
    public bool PrivacyConsent { get; set; }

    public List<Donation> Donations { get; set; } = [];
}
