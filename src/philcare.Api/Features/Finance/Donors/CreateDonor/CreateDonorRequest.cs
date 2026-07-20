using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Donors.CreateDonor;

public sealed record CreateDonorRequest(
    string Name,
    DonorType Type,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes);

public sealed record CreateDonorResponse(
    int Id,
    string Name,
    DonorType Type,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes,
    bool IsActive);
