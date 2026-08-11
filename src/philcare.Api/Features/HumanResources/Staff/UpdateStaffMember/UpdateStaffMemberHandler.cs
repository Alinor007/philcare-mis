using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.HumanResources.Staff.UpdateStaffMember;

public sealed class UpdateStaffMemberHandler(AppDbContext db)
{
    public async Task<Result<UpdateStaffMemberResponse>> HandleAsync(int id, UpdateStaffMemberRequest request, CancellationToken cancellationToken)
    {
        var staffMember = await db.StaffMembers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (staffMember is null)
        {
            return Result.Failure<UpdateStaffMemberResponse>(
                Error.NotFound("Staff.NotFound", "Staff member not found."));
        }

        // Same lookup checks as create — an edit must not be a way around them.
        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var departmentExists = await db.LookupItems.AnyAsync(
                l => l.Category == LookupCategory.OwnerDepartment && l.Code == request.Department && l.IsActive,
                cancellationToken);

            if (!departmentExists)
            {
                return Result.Failure<UpdateStaffMemberResponse>(
                    Error.Validation("Staff.InvalidDepartment", "Department is not a recognised organisational unit."));
            }
        }

        var employmentTypeExists = await db.LookupItems.AnyAsync(
            l => l.Category == LookupCategory.EmploymentType && l.Code == request.EmploymentType && l.IsActive,
            cancellationToken);

        if (!employmentTypeExists)
        {
            return Result.Failure<UpdateStaffMemberResponse>(
                Error.Validation("Staff.InvalidEmploymentType", "Employment type is not a recognised value."));
        }

        staffMember.FullName = request.FullName;
        staffMember.Position = request.Position;
        staffMember.Department = request.Department;
        staffMember.EmploymentType = request.EmploymentType;
        staffMember.HiredDate = request.HiredDate;
        staffMember.Email = request.Email;
        staffMember.Phone = request.Phone;
        staffMember.Notes = request.Notes;
        staffMember.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateStaffMemberResponse(
            staffMember.Id, staffMember.FullName, staffMember.Position, staffMember.Department,
            staffMember.EmploymentType, staffMember.HiredDate, staffMember.IsActive));
    }
}
