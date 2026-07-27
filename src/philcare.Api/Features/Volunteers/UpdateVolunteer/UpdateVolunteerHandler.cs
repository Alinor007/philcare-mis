using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Volunteers.UpdateVolunteer;

public sealed class UpdateVolunteerHandler(AppDbContext db)
{
    public async Task<Result<UpdateVolunteerResponse>> HandleAsync(int id, UpdateVolunteerRequest request, CancellationToken cancellationToken)
    {
        var volunteer = await db.Volunteers.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<UpdateVolunteerResponse>(Error.NotFound("Volunteers.NotFound", "Volunteer not found."));
        }

        volunteer.FullName = request.FullName;
        volunteer.Gender = request.Gender;
        volunteer.Phone = request.Phone;
        volunteer.Email = request.Email;
        volunteer.Barangay = request.Barangay;
        volunteer.City = request.City;
        volunteer.Province = request.Province;
        volunteer.Region = request.Region;
        volunteer.Skills = request.Skills;
        volunteer.Status = request.Status;
        volunteer.OrientationCompleted = request.OrientationCompleted;
        volunteer.OrientationDate = request.OrientationDate;
        volunteer.CodeOfConductSigned = request.CodeOfConductSigned;
        volunteer.CodeOfConductDate = request.CodeOfConductDate;
        volunteer.PoliceClearanceOnFile = request.PoliceClearanceOnFile;
        volunteer.Notes = request.Notes;
        volunteer.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateVolunteerResponse(
            volunteer.Id, volunteer.FullName, volunteer.Gender, volunteer.Status, volunteer.OrientationCompleted, volunteer.IsActive));
    }
}
