namespace philcare.Api.Features.Governance.People.UpdatePerson;

public sealed record UpdatePersonRequest(
    string FullName,
    string PersonCategory,
    string Status,
    string? Email,
    string? ContactNumber,
    bool DefaultVotingRights,
    int? VolunteerId,
    string? Notes,
    bool IsActive);

public sealed record UpdatePersonResponse(int Id, string FullName, string PersonCategory, string Status, bool IsActive);
