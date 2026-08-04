using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Participants.CreateParticipant;

public sealed class CreateParticipantHandler(AppDbContext db)
{
    public async Task<Result<CreateParticipantResponse>> HandleAsync(CreateParticipantRequest request, CancellationToken cancellationToken)
    {
        var participant = new Participant
        {
            FullName = request.FullName,
            ParticipantType = request.ParticipantType,
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
            participant.Status, participant.IsActive));
    }
}
