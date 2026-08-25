using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.People.Domain;

namespace philcare.Api.Features.Governance.People.CreatePerson;

/// <summary>
/// The identity record — one row per human being, regardless of how many roles (staff, volunteer,
/// board member, member) they go on to hold. Role-specific profiles are created separately and
/// point back at this Person's id; this handler knows nothing about them.
/// </summary>
public sealed class CreatePersonHandler(AppDbContext db)
{
    public async Task<Result<CreatePersonResponse>> HandleAsync(CreatePersonRequest request, CancellationToken cancellationToken)
    {
        // Cross-role duplicate gate. This is the payoff of Person unification: one check now
        // covers staff, volunteers and members, instead of each role re-implementing its own (or,
        // as before, none at all). Name alone is never enough — distinct people genuinely share
        // names in these communities — so a match needs a corroborating identifier. Soft: the
        // officer can confirm and proceed, which is why there is no unique index behind it.
        if (!request.ConfirmDuplicate)
        {
            var name = request.FullName.Trim();
            var contact = request.ContactNumber?.Trim();
            var barangay = request.Barangay?.Trim();

            var duplicate = await db.GovernancePeople
                .Where(p => p.IsActive
                    && p.FullName == name
                    && ((contact != null && p.ContactNumber == contact) || (barangay != null && p.Barangay == barangay)))
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (duplicate is not null)
            {
                var matchedOn = !string.IsNullOrWhiteSpace(contact) && duplicate.ContactNumber == contact
                    ? $"contact number {duplicate.ContactNumber}"
                    : $"barangay {duplicate.Barangay}";

                return Result.Failure<CreatePersonResponse>(
                    Error.Conflict("People.PossibleDuplicate",
                        $"{duplicate.FullName} is already registered with the same {matchedOn} "
                        + $"(person #{duplicate.Id}). Confirm to register this as a different person."));
            }
        }

        var person = new Person
        {
            FullName = request.FullName,
            PersonCategory = request.PersonCategory,
            Status = "ACTIVE",
            Email = request.Email,
            ContactNumber = request.ContactNumber,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            CivilStatus = request.CivilStatus,
            Barangay = request.Barangay,
            City = request.City,
            Province = request.Province,
            Region = request.Region,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactNumber = request.EmergencyContactNumber,
            PhotoUrl = request.PhotoUrl,
            DefaultVotingRights = request.DefaultVotingRights,
            Notes = request.Notes,
            IsActive = true
        };

        db.GovernancePeople.Add(person);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreatePersonResponse(person.Id, person.FullName, person.PersonCategory, person.Status, person.IsActive));
    }
}
