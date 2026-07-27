namespace philcare.Api.Features.Partners.UpdatePartner;

public sealed record UpdatePartnerRequest(
    string Name,
    string PartnerType,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    string? City,
    string? Province,
    string? Region,
    string? MouReference,
    DateTime? MouStartDate,
    DateTime? MouEndDate,
    string? AccreditationNotes,
    string? Notes,
    bool IsActive);

public sealed record UpdatePartnerResponse(
    int Id, string Name, string PartnerType, string? ContactPerson, string? Email, string? Phone, bool IsActive);
