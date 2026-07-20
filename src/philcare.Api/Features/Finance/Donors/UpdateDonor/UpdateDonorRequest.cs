using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Donors.UpdateDonor;

public sealed record UpdateDonorRequest(
    string Name,
    DonorType Type,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes,
    bool IsActive);

public sealed record UpdateDonorResponse(
    int Id, string Name, DonorType Type, string? Email, string? Phone, string? Address, string? Notes, bool IsActive);
