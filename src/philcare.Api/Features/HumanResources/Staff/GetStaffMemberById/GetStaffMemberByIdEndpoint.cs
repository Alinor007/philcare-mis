using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.HumanResources.Staff.GetStaffMemberById;

public sealed record StaffMemberDetailResponse(
    int Id,
    int PersonId,
    string FullName,
    string? Email,
    string? ContactNumber,
    string? PhotoUrl,
    string Position,
    string? Department,
    string EmploymentType,
    DateTime? HiredDate,
    int? SupervisorPersonId,
    string? SupervisorName,
    string? Notes,
    bool IsActive);

public sealed class GetStaffMemberByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/staff/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var staffMember = await db.StaffMembers
                .Where(s => s.Id == id)
                .Select(s => new StaffMemberDetailResponse(
                    s.Id, s.PersonId, s.Person.FullName, s.Person.Email, s.Person.ContactNumber, s.Person.PhotoUrl,
                    s.Position, s.Department, s.EmploymentType, s.HiredDate,
                    s.SupervisorPersonId, s.SupervisorPerson != null ? s.SupervisorPerson.FullName : null,
                    s.Notes, s.IsActive))
                .FirstOrDefaultAsync(ct);

            if (staffMember is null)
            {
                return Results.Problem(title: "Staff.NotFound", detail: "Staff member not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(staffMember);
        })
        .WithName("GetStaffMemberById")
        .WithTags("Staff")
        .RequireAuthorization();
    }
}
