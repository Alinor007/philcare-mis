namespace philcare.Api.Features.HumanResources.Staff.UpdateStaffMember;

public sealed record UpdateStaffMemberRequest(
    string FullName,
    string Position,
    string? Department,
    string EmploymentType,
    DateTime? HiredDate,
    string? Email,
    string? Phone,
    string? Notes,
    bool IsActive);

public sealed record UpdateStaffMemberResponse(
    int Id,
    string FullName,
    string Position,
    string? Department,
    string EmploymentType,
    DateTime? HiredDate,
    bool IsActive);
