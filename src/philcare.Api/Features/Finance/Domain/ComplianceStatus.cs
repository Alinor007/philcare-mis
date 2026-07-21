namespace philcare.Api.Features.Finance.Domain;

/// <summary>Know-Your-Donor review status.</summary>
public enum KydStatus
{
    Pending,
    Review,
    Cleared,
    Rejected
}

public enum RiskRating
{
    Low,
    Medium,
    High
}
