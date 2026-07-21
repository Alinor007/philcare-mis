using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Donors.CreateDonor;

public sealed record CreateDonorRequest(
    string Name,
    DonorType Type,
    string? Email,
    string? Phone,
    string? Address,
    string? Country,
    string? Notes,
    RiskRating RiskRating,
    bool PepFlag,
    bool PrivacyConsent);

public sealed record CreateDonorResponse(
    int Id,
    string Name,
    DonorType Type,
    string? Email,
    string? Phone,
    string? Address,
    string? Country,
    string? Notes,
    bool IsActive,
    KydStatus KydStatus,
    RiskRating RiskRating,
    bool PepFlag,
    bool PrivacyConsent);
