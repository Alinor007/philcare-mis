using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.Programs.ActivityParticipants.AddActivityParticipant;

public sealed class AddActivityParticipantHandler(AppDbContext db)
{
    public async Task<Result<AddActivityParticipantResponse>> HandleAsync(
        int activityId, AddActivityParticipantRequest request, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        if (activity is null)
        {
            return Result.Failure<AddActivityParticipantResponse>(Error.NotFound("ActivityParticipants.ActivityNotFound", "Activity not found."));
        }

        var staffMember = await db.StaffMembers.FirstOrDefaultAsync(p => p.Id == request.StaffMemberId, cancellationToken);

        if (staffMember is null)
        {
            return Result.Failure<AddActivityParticipantResponse>(Error.NotFound("ActivityParticipants.StaffMemberNotFound", "Staff member not found."));
        }

        if (!staffMember.IsActive)
        {
            return Result.Failure<AddActivityParticipantResponse>(
                Error.Validation("ActivityParticipants.StaffMemberInactive", "Cannot assign an inactive staff member."));
        }

        // Soft delete means a prior removal leaves the (ActivityId, StaffMemberId) row behind —
        // the unique index means a re-enrollment must reactivate it, never insert a second row.
        var existingLink = await db.ActivityParticipants
            .FirstOrDefaultAsync(ap => ap.ActivityId == activityId && ap.StaffMemberId == request.StaffMemberId, cancellationToken);

        if (existingLink is { IsActive: true })
        {
            return Result.Failure<AddActivityParticipantResponse>(
                Error.Conflict("ActivityParticipants.AlreadyEnrolled", "This staff member is already assigned to this activity."));
        }

        if (!string.IsNullOrWhiteSpace(request.AttendanceStatus))
        {
            var validAttendanceStatus = await db.LookupItems.AnyAsync(
                l => l.Category == LookupCategory.AttendanceStatus && l.Code == request.AttendanceStatus, cancellationToken);

            if (!validAttendanceStatus)
            {
                return Result.Failure<AddActivityParticipantResponse>(
                    Error.Validation("ActivityParticipants.InvalidAttendanceStatus", "Attendance status must be a valid attendance_status lookup code."));
            }
        }

        // No consent check: this roster now holds staff, and the gate compared ConsentRequired
        // against a beneficiary's ConsentOnFile, which StaffMember does not carry. The flag is
        // still stored on the row (see ActivityParticipant) but nothing enforces it.

        ActivityParticipant link;

        if (existingLink is not null)
        {
            existingLink.RoleInActivity = request.RoleInActivity;
            existingLink.AttendanceStatus = request.AttendanceStatus;
            existingLink.ConsentRequired = request.ConsentRequired;
            existingLink.EvidenceLink = request.EvidenceLink;
            existingLink.Remarks = request.Remarks;
            existingLink.IsActive = true;
            link = existingLink;
        }
        else
        {
            link = new ActivityParticipant
            {
                ActivityId = activityId,
                StaffMemberId = request.StaffMemberId,
                RoleInActivity = request.RoleInActivity,
                AttendanceStatus = request.AttendanceStatus,
                ConsentRequired = request.ConsentRequired,
                EvidenceLink = request.EvidenceLink,
                Remarks = request.Remarks
            };
            db.ActivityParticipants.Add(link);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new AddActivityParticipantResponse(
            link.Id, link.ActivityId, link.StaffMemberId, staffMember.FullName, link.RoleInActivity, link.AttendanceStatus));
    }
}
