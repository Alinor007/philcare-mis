namespace philcare.Api.Features.HumanResources.Staff.UpdateStaffMember;

/// <summary>No PersonId here — a staff profile stays attached to the Person it was created for;
/// there is no "reassign to a different person" operation.</summary>
public sealed record UpdateStaffMemberRequest(
    string Position,
    string? Department,
    string EmploymentType,
    DateTime? HiredDate,
    int? SupervisorPersonId,
    string? Notes,
    bool IsActive);

public sealed record UpdateStaffMemberResponse(
    int Id,
    int PersonId,
    string FullName,
    string Position,
    string? Department,
    string EmploymentType,
    DateTime? HiredDate,
    bool IsActive);
