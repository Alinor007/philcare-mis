using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Api;
using philcare.Api.Common.Persistence;
using philcare.Api.Common.Domain;

namespace philcare.Api.Features.HumanResources.Volunteers.GetVolunteerById;

/// <summary>Identity fields are read through the Person this profile belongs to.</summary>
public sealed record VolunteerDetailResponse(
    int Id,
    int PersonId,
    string FullName,
    Gender Gender,
    string? Phone,
    string? Email,
    string? Barangay,
    string? City,
    string? Province,
    string? Region,
    string? PhotoUrl,
    string? Skills,
    string? AvailabilityDays,
    string Status,
    bool OrientationCompleted,
    DateTime? OrientationDate,
    bool CodeOfConductSigned,
    DateTime? CodeOfConductDate,
    bool PoliceClearanceOnFile,
    string? Notes,
    bool IsActive,
    int ActivityCount);

public sealed class GetVolunteerByIdEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/volunteers/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var volunteer = await db.Volunteers
                .Where(v => v.Id == id)
                .Select(v => new VolunteerDetailResponse(
                    v.Id, v.PersonId, v.Person.FullName, v.Person.Gender, v.Person.ContactNumber, v.Person.Email,
                    v.Person.Barangay, v.Person.City, v.Person.Province, v.Person.Region, v.Person.PhotoUrl,
                    v.Skills, v.AvailabilityDays, v.Status,
                    v.OrientationCompleted, v.OrientationDate, v.CodeOfConductSigned, v.CodeOfConductDate, v.PoliceClearanceOnFile,
                    v.Notes, v.IsActive, v.ActivityVolunteers.Count))
                .FirstOrDefaultAsync(ct);

            if (volunteer is null)
            {
                return Results.Problem(title: "Volunteers.NotFound", detail: "Volunteer not found.", statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(volunteer);
        })
        .WithName("GetVolunteerById")
        .WithTags("Volunteers")
        .RequireAuthorization();
    }
}
