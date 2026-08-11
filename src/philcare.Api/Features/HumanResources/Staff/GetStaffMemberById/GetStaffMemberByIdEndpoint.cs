using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.HumanResources.Staff.GetStaffMemberById;

public sealed record StaffMemberDetailResponse(
    int Id,
    string FullName,
    string Position,
    string? Department,
    string EmploymentType,
    DateTime? HiredDate,
    string? Email,
    string? Phone,
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
                    s.Id, s.FullName, s.Position, s.Department, s.EmploymentType, s.HiredDate,
                    s.Email, s.Phone, s.Notes, s.IsActive))
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
