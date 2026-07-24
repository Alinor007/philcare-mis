namespace philcare.Api.Features.Partners.CreatePartner;

public sealed record CreatePartnerRequest(
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
    string? Notes);

public sealed record CreatePartnerResponse(
    int Id, string Name, string PartnerType, string? ContactPerson, string? Email, string? Phone, bool IsActive);
