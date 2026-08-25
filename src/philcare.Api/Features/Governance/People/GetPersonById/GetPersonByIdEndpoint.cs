using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.People.GetPersonById;

public sealed record PersonDetailResponse(
    int Id,
    string FullName,
    string PersonCategory,
    string Status,
    string? Email,
    string? ContactNumber,
    string? DateOfBirth,
    Gender Gender,
    string? CivilStatus,
    string? Barangay,
    string? City,
    string? Province,
    string? Region,
    string? EmergencyContactName,
    string? EmergencyContactNumber,
    string? PhotoUrl,
    bool DefaultVotingRights,
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
                    p.Id, p.FullName, p.PersonCategory, p.Status, p.Email, p.ContactNumber,
                    p.DateOfBirth, p.Gender, p.CivilStatus, p.Barangay, p.City, p.Province, p.Region,
                    p.EmergencyContactName, p.EmergencyContactNumber, p.PhotoUrl,
                    p.DefaultVotingRights, p.Notes, p.IsActive, p.Assignments.Count))
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
