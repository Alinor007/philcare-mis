using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.HumanResources.Staff.DeactivateStaffMember;

public sealed class DeactivateStaffMemberEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        // DELETE verb, soft-deactivate semantics — the same shape as DeactivatePartner. Employment
        // history is never hard-deleted; the row stays for audit and past-attribution.
        app.MapDelete("/api/staff/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var staffMember = await db.StaffMembers.FirstOrDefaultAsync(s => s.Id == id, ct);

            if (staffMember is null)
            {
                return Results.Problem(title: "Staff.NotFound", detail: "Staff member not found.", statusCode: StatusCodes.Status404NotFound);
            }

            staffMember.IsActive = false;
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeactivateStaffMember")
        .WithTags("Staff")
        .RequireAuthorization("Admin");
    }
}
