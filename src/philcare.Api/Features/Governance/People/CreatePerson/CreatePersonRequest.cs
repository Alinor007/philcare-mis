namespace philcare.Api.Features.Governance.People.CreatePerson;

public sealed record CreatePersonRequest(
    string FullName,
    string PersonCategory,
    string? Email,
    string? ContactNumber,
    bool DefaultVotingRights,
    int? VolunteerId,
    string? Notes);

public sealed record CreatePersonResponse(int Id, string FullName, string PersonCategory, string Status, bool IsActive);
