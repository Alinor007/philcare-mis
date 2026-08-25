using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.Programs.ActivityParticipants.UpdateActivityParticipant;

/// <summary>
/// Updates an existing roster entry (attendance, role, evidence) in place — added so recording
/// attendance after enrollment no longer requires DELETE + re-POST, which destroyed the audit trail.
/// </summary>
public sealed class UpdateActivityParticipantHandler(AppDbContext db)
{
    public async Task<Result<UpdateActivityParticipantResponse>> HandleAsync(
        int activityId, int staffMemberId, UpdateActivityParticipantRequest request, CancellationToken cancellationToken)
    {
        var link = await db.ActivityParticipants
            .Include(ap => ap.StaffMember).ThenInclude(s => s.Person)
            .FirstOrDefaultAsync(ap => ap.ActivityId == activityId && ap.StaffMemberId == staffMemberId && ap.IsActive, cancellationToken);

        if (link is null)
        {
            return Result.Failure<UpdateActivityParticipantResponse>(
                Error.NotFound("ActivityParticipants.NotFound", "This beneficiary is not enrolled in this activity."));
        }

        if (!string.IsNullOrWhiteSpace(request.AttendanceStatus))
        {
            var validAttendanceStatus = await db.LookupItems.AnyAsync(
                l => l.Category == LookupCategory.AttendanceStatus && l.Code == request.AttendanceStatus, cancellationToken);

            if (!validAttendanceStatus)
            {
                return Result.Failure<UpdateActivityParticipantResponse>(
                    Error.Validation("ActivityParticipants.InvalidAttendanceStatus", "Attendance status must be a valid attendance_status lookup code."));
            }
        }

        // Consent is no longer checked here — see AddActivityParticipantHandler. StaffMember has
        // no ConsentOnFile; the flag is stored but unenforced.

        link.RoleInActivity = request.RoleInActivity;
        link.AttendanceStatus = request.AttendanceStatus;
        link.ConsentRequired = request.ConsentRequired;
        link.EvidenceLink = request.EvidenceLink;
        link.Remarks = request.Remarks;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateActivityParticipantResponse(
            link.Id, link.ActivityId, link.StaffMemberId, link.StaffMember.Person.FullName, link.RoleInActivity, link.AttendanceStatus));
    }
}
