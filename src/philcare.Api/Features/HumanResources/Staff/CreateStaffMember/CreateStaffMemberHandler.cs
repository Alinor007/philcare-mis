using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.HumanResources.Domain;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.HumanResources.Staff.CreateStaffMember;

public sealed class CreateStaffMemberHandler(AppDbContext db)
{
    public async Task<Result<CreateStaffMemberResponse>> HandleAsync(CreateStaffMemberRequest request, CancellationToken cancellationToken)
    {
        // Both coded fields are validated against the lookup table, including IsActive, following
        // CreateOtherIncomeHandler. This is stricter than Activity.ResponsibleDepartment, which is
        // length-only — that field predates the convention and holds legacy free text, so it is
        // deliberately left alone rather than tightened here.
        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var departmentExists = await db.LookupItems.AnyAsync(
                l => l.Category == LookupCategory.OwnerDepartment && l.Code == request.Department && l.IsActive,
                cancellationToken);

            if (!departmentExists)
            {
                return Result.Failure<CreateStaffMemberResponse>(
                    Error.Validation("Staff.InvalidDepartment", "Department is not a recognised organisational unit."));
            }
        }

        var employmentTypeExists = await db.LookupItems.AnyAsync(
            l => l.Category == LookupCategory.EmploymentType && l.Code == request.EmploymentType && l.IsActive,
            cancellationToken);

        if (!employmentTypeExists)
        {
            return Result.Failure<CreateStaffMemberResponse>(
                Error.Validation("Staff.InvalidEmploymentType", "Employment type is not a recognised value."));
        }

        // No duplicate-name check, unlike CreatePartnerHandler: two employees can share a name.
        var staffMember = new StaffMember
        {
            FullName = request.FullName,
            Position = request.Position,
            Department = request.Department,
            EmploymentType = request.EmploymentType,
            HiredDate = request.HiredDate,
            Email = request.Email,
            Phone = request.Phone,
            Notes = request.Notes,
            IsActive = true
        };

        db.StaffMembers.Add(staffMember);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateStaffMemberResponse(
            staffMember.Id, staffMember.FullName, staffMember.Position, staffMember.Department,
            staffMember.EmploymentType, staffMember.HiredDate, staffMember.IsActive));
    }
}
