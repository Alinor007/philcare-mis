using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Participants.CreateParticipant;

public sealed class CreateParticipantHandler(AppDbContext db)
{
    public async Task<Result<CreateParticipantResponse>> HandleAsync(CreateParticipantRequest request, CancellationToken cancellationToken)
    {
        if (!request.ConsentOnFile)
        {
            return Result.Failure<CreateParticipantResponse>(
                Error.Validation("Participants.ConsentRequired", "Consent must be on file before a participant can be registered."));
        }

        // Elevated safeguarding risk does not block registration — it saves the record and
        // surfaces a warning the officer must act on (see Volunteers' orientation gate for the
        // analogous check on the activity-enrollment side).
        var hasSafeguardingRisk = !string.IsNullOrWhiteSpace(request.SafeguardingCategory)
            && !string.Equals(request.SafeguardingCategory, "NONE", StringComparison.OrdinalIgnoreCase);

        var participant = new Participant
        {
            FullName = request.FullName,
            BeneficiaryType = request.BeneficiaryType,
            Gender = request.Gender,
            AgeGroup = request.AgeGroup,
            Phone = request.Phone,
            Barangay = request.Barangay,
            City = request.City,
            Province = request.Province,
            Region = request.Region,
            Country = request.Country,
            VulnerabilityCategory = request.VulnerabilityCategory,
            SafeguardingCategory = request.SafeguardingCategory,
            ConsentOnFile = request.ConsentOnFile,
            Status = "PENDING",
            Remarks = request.Remarks,
            IsActive = true
        };

        db.Participants.Add(participant);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateParticipantResponse(
            participant.Id, participant.FullName, participant.ParticipantType, participant.BeneficiaryType, participant.Gender,
            participant.VulnerabilityCategory, participant.SafeguardingCategory, participant.ConsentOnFile,
            participant.Status, participant.IsActive, hasSafeguardingRisk,
            hasSafeguardingRisk ? "Elevated safeguarding risk — officer must be notified." : null));
    }
}
