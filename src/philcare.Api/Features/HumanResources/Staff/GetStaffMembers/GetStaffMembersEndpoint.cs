using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.HumanResources.Staff.GetStaffMembers;

public sealed record StaffMemberListItemResponse(
    int Id,
    string FullName,
    string Position,
    string? Department,
    string EmploymentType,
    DateTime? HiredDate,
    bool IsActive);

public sealed class GetStaffMembersEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/staff", async (
            string? department,
            string? employmentType,
            string? search,
            bool? includeInactive,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var query = db.StaffMembers.AsQueryable();

            // Soft-deleted rows are hidden unless explicitly asked for, per the repo-wide rule.
            if (includeInactive != true)
            {
                query = query.Where(s => s.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(s => s.Department == department);
            }

            if (!string.IsNullOrWhiteSpace(employmentType))
            {
                query = query.Where(s => s.EmploymentType == employmentType);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.FullName.Contains(search) || s.Position.Contains(search));
            }

            var staff = await query
                .OrderBy(s => s.FullName)
                .Select(s => new StaffMemberListItemResponse(
                    s.Id, s.FullName, s.Position, s.Department, s.EmploymentType, s.HiredDate, s.IsActive))
                .ToListAsync(ct);

            return Results.Ok(staff);
        })
        .WithName("GetStaffMembers")
        .WithTags("Staff")
        .RequireAuthorization();
    }
}
