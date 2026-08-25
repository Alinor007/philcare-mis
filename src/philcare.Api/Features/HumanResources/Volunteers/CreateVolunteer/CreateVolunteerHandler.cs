using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.HumanResources.Domain;

namespace philcare.Api.Features.HumanResources.Volunteers.CreateVolunteer;

public sealed class CreateVolunteerHandler(AppDbContext db)
{
    public async Task<Result<CreateVolunteerResponse>> HandleAsync(CreateVolunteerRequest request, CancellationToken cancellationToken)
    {
        var person = await db.GovernancePeople.FirstOrDefaultAsync(p => p.Id == request.PersonId, cancellationToken);

        if (person is null)
        {
            return Result.Failure<CreateVolunteerResponse>(Error.NotFound("Volunteers.PersonNotFound", "Person not found."));
        }

        if (!person.IsActive)
        {
            return Result.Failure<CreateVolunteerResponse>(
                Error.Validation("Volunteers.PersonInactive", "Cannot create a volunteer profile for an inactive person."));
        }

        // One volunteer profile per person — mirrors the unique index on PersonId, checked here
        // first so the caller gets a clean conflict rather than a raw 500.
        var alreadyVolunteer = await db.Volunteers.AnyAsync(v => v.PersonId == request.PersonId, cancellationToken);

        if (alreadyVolunteer)
        {
            return Result.Failure<CreateVolunteerResponse>(
                Error.Conflict("Volunteers.AlreadyVolunteer", "This person already has a volunteer profile."));
        }

        var volunteer = new Volunteer
        {
            PersonId = request.PersonId,
            Skills = request.Skills,
            AvailabilityDays = request.AvailabilityDays,
            Status = "ACTIVE",
            OrientationCompleted = request.OrientationCompleted,
            OrientationDate = request.OrientationDate,
            CodeOfConductSigned = request.CodeOfConductSigned,
            CodeOfConductDate = request.CodeOfConductDate,
            PoliceClearanceOnFile = request.PoliceClearanceOnFile,
            Notes = request.Notes,
            IsActive = true
        };

        db.Volunteers.Add(volunteer);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateVolunteerResponse(
            volunteer.Id, volunteer.PersonId, person.FullName, volunteer.Status,
            volunteer.OrientationCompleted, volunteer.IsActive));
    }
}
