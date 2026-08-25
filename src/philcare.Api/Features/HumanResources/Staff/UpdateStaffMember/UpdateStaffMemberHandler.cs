using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.HumanResources.Staff.UpdateStaffMember;

public sealed class UpdateStaffMemberHandler(AppDbContext db)
{
    public async Task<Result<UpdateStaffMemberResponse>> HandleAsync(int id, UpdateStaffMemberRequest request, CancellationToken cancellationToken)
    {
        var staffMember = await db.StaffMembers
            .Include(s => s.Person)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (staffMember is null)
        {
            return Result.Failure<UpdateStaffMemberResponse>(
                Error.NotFound("Staff.NotFound", "Staff member not found."));
        }

        if (request.SupervisorPersonId is not null)
        {
            if (request.SupervisorPersonId == staffMember.PersonId)
            {
                return Result.Failure<UpdateStaffMemberResponse>(
                    Error.Validation("Staff.CannotSuperviseSelf", "A person cannot be their own supervisor."));
            }

            var supervisorExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.SupervisorPersonId, cancellationToken);

            if (!supervisorExists)
            {
                return Result.Failure<UpdateStaffMemberResponse>(Error.NotFound("Staff.SupervisorNotFound", "Supervisor not found."));
            }
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

        staffMember.Position = request.Position;
        staffMember.Department = request.Department;
        staffMember.EmploymentType = request.EmploymentType;
        staffMember.HiredDate = request.HiredDate;
        staffMember.SupervisorPersonId = request.SupervisorPersonId;
        staffMember.Notes = request.Notes;
        staffMember.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateStaffMemberResponse(
            staffMember.Id, staffMember.PersonId, staffMember.Person.FullName, staffMember.Position,
            staffMember.Department, staffMember.EmploymentType, staffMember.HiredDate, staffMember.IsActive));
    }
}
