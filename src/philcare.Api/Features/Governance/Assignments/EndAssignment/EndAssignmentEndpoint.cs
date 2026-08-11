using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Behaviors;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.Assignments.EndAssignment;

public sealed record EndAssignmentRequest(DateTime? EndDate);
public sealed record EndAssignmentResponse(int Id, string Status, DateTime? EndDate);

public sealed class EndAssignmentEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/governance/assignments/{id:int}/end", async (int id, EndAssignmentRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.Id == id, ct);

            if (assignment is null)
            {
                return Results.Problem(title: "Governance.AssignmentNotFound", detail: "Assignment not found.", statusCode: StatusCodes.Status404NotFound);
            }

            if (assignment.Status == AssignmentStatus.Former)
            {
                return Results.Problem(
                    title: "Governance.AssignmentAlreadyEnded", detail: "This assignment has already ended.", statusCode: StatusCodes.Status409Conflict);
            }

            assignment.Status = AssignmentStatus.Former;
            assignment.EndDate = request.EndDate ?? DateTime.UtcNow.Date;
            assignment.IsPrimaryCurrent = null;

            await db.SaveChangesAsync(ct);

            return Results.Ok(new EndAssignmentResponse(assignment.Id, assignment.Status.ToString(), assignment.EndDate));
        })
        .WithName("EndAssignment")
        .WithTags("Governance")
        .AddEndpointFilter<ValidationFilter<EndAssignmentRequest>>()
        .RequireAuthorization("Admin");
    }
}
