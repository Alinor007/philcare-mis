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
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Donation> Donations { get; set; } = [];
}
