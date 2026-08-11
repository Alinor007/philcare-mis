using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Governance.Domain;

namespace philcare.Api.Features.Governance.People.CreatePerson;

public sealed class CreatePersonHandler(AppDbContext db)
{
    public async Task<Result<CreatePersonResponse>> HandleAsync(CreatePersonRequest request, CancellationToken cancellationToken)
    {
        if (request.VolunteerId is not null)
        {
            var volunteerExists = await db.Volunteers.AnyAsync(v => v.Id == request.VolunteerId, cancellationToken);

            if (!volunteerExists)
            {
                return Result.Failure<CreatePersonResponse>(Error.NotFound("Governance.VolunteerNotFound", "Volunteer not found."));
            }
        }

        var person = new Person
        {
            FullName = request.FullName,
            PersonCategory = request.PersonCategory,
            Status = "ACTIVE",
            Email = request.Email,
            ContactNumber = request.ContactNumber,
            DefaultVotingRights = request.DefaultVotingRights,
            VolunteerId = request.VolunteerId,
            Notes = request.Notes,
            IsActive = true
        };

        db.GovernancePeople.Add(person);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreatePersonResponse(person.Id, person.FullName, person.PersonCategory, person.Status, person.IsActive));
    }
}
