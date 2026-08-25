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

        person.FullName = request.FullName;
        person.PersonCategory = request.PersonCategory;
        person.Status = request.Status;
        person.Email = request.Email;
        person.ContactNumber = request.ContactNumber;
        person.DateOfBirth = request.DateOfBirth;
        person.Gender = request.Gender;
        person.CivilStatus = request.CivilStatus;
        person.Barangay = request.Barangay;
        person.City = request.City;
        person.Province = request.Province;
        person.Region = request.Region;
        person.EmergencyContactName = request.EmergencyContactName;
        person.EmergencyContactNumber = request.EmergencyContactNumber;
        person.PhotoUrl = request.PhotoUrl;
        person.DefaultVotingRights = request.DefaultVotingRights;
        person.Notes = request.Notes;
        person.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdatePersonResponse(person.Id, person.FullName, person.PersonCategory, person.Status, person.IsActive));
    }
}
