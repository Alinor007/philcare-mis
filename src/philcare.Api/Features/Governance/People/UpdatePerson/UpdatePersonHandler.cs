using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Governance.People.UpdatePerson;

public sealed class UpdatePersonHandler(AppDbContext db)
{
    public async Task<Result<UpdatePersonResponse>> HandleAsync(int id, UpdatePersonRequest request, CancellationToken cancellationToken)
    {
        var person = await db.GovernancePeople.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (person is null)
        {
            return Result.Failure<UpdatePersonResponse>(Error.NotFound("Governance.PersonNotFound", "Person not found."));
        }

        if (request.VolunteerId is not null)
        {
            var volunteerExists = await db.Volunteers.AnyAsync(v => v.Id == request.VolunteerId, cancellationToken);

            if (!volunteerExists)
            {
                return Result.Failure<UpdatePersonResponse>(Error.NotFound("Governance.VolunteerNotFound", "Volunteer not found."));
            }
        }

        person.FullName = request.FullName;
        person.PersonCategory = request.PersonCategory;
        person.Status = request.Status;
        person.Email = request.Email;
        person.ContactNumber = request.ContactNumber;
        person.DefaultVotingRights = request.DefaultVotingRights;
        person.VolunteerId = request.VolunteerId;
        person.Notes = request.Notes;
        person.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdatePersonResponse(person.Id, person.FullName, person.PersonCategory, person.Status, person.IsActive));
    }
}
