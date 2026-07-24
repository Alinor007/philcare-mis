using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.Programs.Activities.CreateActivity;

public sealed class CreateActivityHandler(AppDbContext db)
{
    public async Task<Result<CreateActivityResponse>> HandleAsync(CreateActivityRequest request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<CreateActivityResponse>(Error.NotFound("Activities.ProjectNotFound", "Project not found."));
        }

        if (!project.IsActive)
        {
            return Result.Failure<CreateActivityResponse>(
                Error.Validation("Activities.ProjectInactive", "Cannot create an activity under an inactive project."));
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
                return Result.Failure<CreateActivityResponse>(Error.NotFound("Activities.PartnerNotFound", "Partner not found."));
            }

            if (!partner.IsActive)
            {
                return Result.Failure<CreateActivityResponse>(
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
                return Result.Failure<CreateActivityResponse>(
                    Error.Validation("Activities.InvalidSafeguardingRisk", "Safeguarding risk must be a valid safeguarding_category lookup code."));
            }
        }

        var activity = new Activity
        {
            ProjectId = project.Id,
            Name = request.Name,
            ActivityCategory = request.ActivityCategory,
            ActivityType = request.ActivityType,
            TargetGroup = request.TargetGroup,
            Barangay = request.Barangay,
            City = request.City,
            Province = request.Province,
            Region = request.Region,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Budget = request.Budget,
            ImplementingPartner = implementingPartnerName,
            ImplementingPartnerId = request.ImplementingPartnerId,
            ResponsibleDepartment = request.ResponsibleDepartment,
            SdgAlignment = request.SdgAlignment,
            ImplementationStatus = "PLANNED",
            SafeguardingRisk = request.SafeguardingRisk,
            EvidenceLink = request.EvidenceLink,
            Notes = request.Notes,
            IsActive = true
        };

        db.Activities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateActivityResponse(
            activity.Id, activity.ProjectId, activity.Name, activity.ActivityCategory, activity.ActivityType,
            activity.TargetGroup, activity.Budget, activity.ImplementationStatus, activity.IsActive));
    }
}
