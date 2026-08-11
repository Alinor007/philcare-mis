namespace philcare.Api.Features.HumanResources.Staff.CreateStaffMember;

public sealed record CreateStaffMemberRequest(
    string FullName,
    string Position,
    string? Department,
    string EmploymentType,
    DateTime? HiredDate,
    string? Email,
    string? Phone,
    string? Notes);

public sealed record CreateStaffMemberResponse(
    int Id,
    string FullName,
    string Position,
    string? Department,
    string EmploymentType,
    DateTime? HiredDate,
    bool IsActive);
