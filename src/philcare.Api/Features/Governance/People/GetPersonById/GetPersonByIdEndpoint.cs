using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.People.GetPersonById;

public sealed record PersonDetailResponse(
    int Id,
    string FullName,
    string PersonCategory,
    string Status,
    string? Email,
    string? ContactNumber,
    bool DefaultVotingRights,
    int? VolunteerId,
    string? Notes,
    bool IsActive,
    int AssignmentCount);

public sealed class GetPersonByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/governance/people/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var person = await db.GovernancePeople
                .Where(p => p.Id == id)
                .Select(p => new PersonDetailResponse(
                    p.Id, p.FullName, p.PersonCategory, p.Status, p.Email, p.ContactNumber, p.DefaultVotingRights,
                    p.VolunteerId, p.Notes, p.IsActive, p.Assignments.Count))
                .FirstOrDefaultAsync(ct);

            if (person is null)
            {
                return Results.Problem(title: "Governance.PersonNotFound", detail: "Person not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(person);
        })
        .WithName("GetPersonById")
        .WithTags("Governance")
        .RequireAuthorization();
    }
}
