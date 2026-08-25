namespace philcare.Api.Features.HumanResources.Staff.CreateStaffMember;

/// <summary>
/// No FullName/Email/Phone/PhotoUrl — those live on the Person this profile is attached to.
/// The officer picks (or, in the UI, creates) a Person first, then fills in these
/// employment-specific fields.
/// </summary>
public sealed record CreateStaffMemberRequest(
    int PersonId,
    string Position,
    string? Department,
    string EmploymentType,
    DateTime? HiredDate,
    int? SupervisorPersonId,
    string? Notes);

public sealed record CreateStaffMemberResponse(
    int Id,
    int PersonId,
    string FullName,
    string Position,
    string? Department,
    string EmploymentType,
    DateTime? HiredDate,
    bool IsActive);
