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
        var person = await db.GovernancePeople.FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);

        if (person is null)
        {
            return Result.Failure<CreateStaffMemberResponse>(Error.NotFound("Staff.PersonNotFound", "Person not found."));
        }

        if (!person.IsActive)
        {
            return Result.Failure<CreateStaffMemberResponse>(
                Error.Validation("Staff.PersonInactive", "Cannot create a staff profile for an inactive person."));
        }

        // One staff profile per person — the same constraint the unique index on PersonId enforces
        // at the DB level; checked here first for a clean error rather than a raw 500.
        var alreadyStaff = await db.StaffMembers.AnyAsync(s => s.PersonId == request.PersonId, cancellationToken);

        if (alreadyStaff)
        {
            return Result.Failure<CreateStaffMemberResponse>(
                Error.Conflict("Staff.AlreadyStaff", "This person already has a staff profile."));
        }

        if (request.SupervisorPersonId is not null)
        {
            if (request.SupervisorPersonId == request.PersonId)
            {
                return Result.Failure<CreateStaffMemberResponse>(
                    Error.Validation("Staff.CannotSuperviseSelf", "A person cannot be their own supervisor."));
            }

            var supervisorExists = await db.GovernancePeople.AnyAsync(p => p.Id == request.SupervisorPersonId, cancellationToken);

            if (!supervisorExists)
            {
                return Result.Failure<CreateStaffMemberResponse>(Error.NotFound("Staff.SupervisorNotFound", "Supervisor not found."));
            }
        }

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

        var staffMember = new StaffMember
        {
            PersonId = request.PersonId,
            Position = request.Position,
            Department = request.Department,
            EmploymentType = request.EmploymentType,
            HiredDate = request.HiredDate,
            SupervisorPersonId = request.SupervisorPersonId,
            Notes = request.Notes,
            IsActive = true
        };

        db.StaffMembers.Add(staffMember);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateStaffMemberResponse(
            staffMember.Id, staffMember.PersonId, person.FullName, staffMember.Position, staffMember.Department,
            staffMember.EmploymentType, staffMember.HiredDate, staffMember.IsActive));
    }
}
