using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.Programs.Activities.UpdateActivity;

public sealed class UpdateActivityHandler(AppDbContext db)
{
    public async Task<Result<UpdateActivityResponse>> HandleAsync(int id, UpdateActivityRequest request, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (activity is null)
        {
            return Result.Failure<UpdateActivityResponse>(Error.NotFound("Activities.NotFound", "Activity not found."));
        }

        // ImplementingPartner (string) is server-derived from Partner.Name when the FK is set, rather
        // than trusting the client's string, so the two fields can't silently drift before the legacy
        // string column is dropped in Sprint 5.
        var implementingPartnerName = request.ImplementingPartner;

        if (request.ImplementingPartnerId is not null)
        {
            var partner = await db.Partners.FirstOrDefaultAsync(p => p.Id == request.ImplementingPartnerId, cancellationToken);

            if (partner is null)
            {
                return Result.Failure<UpdateActivityResponse>(Error.NotFound("Activities.PartnerNotFound", "Partner not found."));
            }

            if (!partner.IsActive)
            {
                return Result.Failure<UpdateActivityResponse>(
                    Error.Validation("Activities.PartnerInactive", "Cannot link an activity to an inactive partner."));
            }

            implementingPartnerName = partner.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.SafeguardingRisk))
        {
            var validSafeguardingRisk = await db.LookupItems.AnyAsync(
                l => l.Category == LookupCategory.SafeguardingCategory && l.Code == request.SafeguardingRisk, cancellationToken);

            if (!validSafeguardingRisk)
            {
                return Result.Failure<UpdateActivityResponse>(
                    Error.Validation("Activities.InvalidSafeguardingRisk", "Safeguarding risk must be a valid safeguarding_category lookup code."));
            }
        }

        activity.Name = request.Name;
        activity.ActivityCategory = request.ActivityCategory;
        activity.ActivityType = request.ActivityType;
        activity.TargetGroup = request.TargetGroup;
        activity.Barangay = request.Barangay;
        activity.City = request.City;
        activity.Province = request.Province;
        activity.Region = request.Region;
        activity.StartDate = request.StartDate;
        activity.EndDate = request.EndDate;
        activity.Budget = request.Budget;
        activity.ImplementingPartner = implementingPartnerName;
        activity.ImplementingPartnerId = request.ImplementingPartnerId;
        activity.ResponsibleDepartment = request.ResponsibleDepartment;
        activity.SdgAlignment = request.SdgAlignment;
        activity.ImplementationStatus = request.ImplementationStatus;
        activity.SafeguardingRisk = request.SafeguardingRisk;
        activity.EvidenceLink = request.EvidenceLink;
        activity.Notes = request.Notes;
        activity.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateActivityResponse(
            activity.Id, activity.ProjectId, activity.Name, activity.ActivityType, activity.Budget, activity.ImplementationStatus, activity.IsActive));
    }
}
