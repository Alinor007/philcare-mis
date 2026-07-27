using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Volunteers.Domain;

namespace philcare.Api.Features.Volunteers.CreateVolunteer;

public sealed class CreateVolunteerHandler(AppDbContext db)
{
    public async Task<Result<CreateVolunteerResponse>> HandleAsync(CreateVolunteerRequest request, CancellationToken cancellationToken)
    {
        var volunteer = new Volunteer
        {
            FullName = request.FullName,
            Gender = request.Gender,
            Phone = request.Phone,
            Email = request.Email,
            Barangay = request.Barangay,
            City = request.City,
            Province = request.Province,
            Region = request.Region,
            Skills = request.Skills,
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
            volunteer.Id, volunteer.FullName, volunteer.Gender, volunteer.Status, volunteer.OrientationCompleted, volunteer.IsActive));
    }
}
