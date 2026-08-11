using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.HumanResources.Domain;

namespace philcare.Api.Features.HumanResources.Volunteers.ActivityVolunteers.AddActivityVolunteer;

public sealed class AddActivityVolunteerHandler(AppDbContext db)
{
    public async Task<Result<AddActivityVolunteerResponse>> HandleAsync(
        int activityId, AddActivityVolunteerRequest request, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity is null)
        {
            return Result.Failure<AddActivityVolunteerResponse>(Error.NotFound("ActivityVolunteers.ActivityNotFound", "Activity not found."));
        }

        var volunteer = await db.Volunteers.FirstOrDefaultAsync(v => v.Id == request.VolunteerId, cancellationToken);

        if (volunteer is null)
        {
            return Result.Failure<AddActivityVolunteerResponse>(Error.NotFound("ActivityVolunteers.VolunteerNotFound", "Volunteer not found."));
        }

        if (!volunteer.IsActive)
        {
            return Result.Failure<AddActivityVolunteerResponse>(
                Error.Validation("ActivityVolunteers.VolunteerInactive", "Cannot enroll an inactive volunteer."));
        }

        var alreadyEnrolled = await db.ActivityVolunteers
            .AnyAsync(av => av.ActivityId == activityId && av.VolunteerId == request.VolunteerId, cancellationToken);

        if (alreadyEnrolled)
        {
            return Result.Failure<AddActivityVolunteerResponse>(
                Error.Conflict("ActivityVolunteers.AlreadyEnrolled", "This volunteer is already enrolled in this activity."));
        }

        // Confirmed policy: ANY safeguarding risk tier (not just High) blocks an unoriented volunteer —
        // a deliberately conservative posture, not an oversight. SafeguardingRisk is validated against
        // the safeguarding_category lookup at Activity create/update time, so a typo can't silently
        // widen or narrow this gate.
        var hasSafeguardingRisk = !string.IsNullOrWhiteSpace(activity.SafeguardingRisk)
            && !string.Equals(activity.SafeguardingRisk, "NONE", StringComparison.OrdinalIgnoreCase);

        if (hasSafeguardingRisk && !volunteer.OrientationCompleted)
        {
            return Result.Failure<AddActivityVolunteerResponse>(
                Error.Validation("ActivityVolunteers.OrientationRequired",
                    "This activity carries a safeguarding risk; the volunteer must complete safeguarding orientation before enrollment."));
        }

        var link = new ActivityVolunteer
        {
            ActivityId = activityId,
            VolunteerId = request.VolunteerId,
            RoleInActivity = request.RoleInActivity,
            AttendanceStatus = request.AttendanceStatus,
            HoursServed = request.HoursServed,
            Remarks = request.Remarks
        };

        db.ActivityVolunteers.Add(link);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new AddActivityVolunteerResponse(
            link.Id, link.ActivityId, link.VolunteerId, volunteer.FullName, link.RoleInActivity, link.AttendanceStatus));
    }
}
