using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Participants.UpdateParticipant;

public sealed class UpdateParticipantHandler(AppDbContext db)
{
    public async Task<Result<UpdateParticipantResponse>> HandleAsync(int id, UpdateParticipantRequest request, CancellationToken cancellationToken)
    {
        var participant = await db.Participants.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (participant is null)
        {
            return Result.Failure<UpdateParticipantResponse>(Error.NotFound("Participants.NotFound", "Participant not found."));
        }

        participant.FullName = request.FullName;
        participant.ParticipantType = request.ParticipantType;
        participant.BeneficiaryType = request.BeneficiaryType;
        participant.Gender = request.Gender;
        participant.AgeGroup = request.AgeGroup;
        participant.Phone = request.Phone;
        participant.Barangay = request.Barangay;
        participant.City = request.City;
        participant.Province = request.Province;
        participant.Region = request.Region;
        participant.Country = request.Country;
        participant.VulnerabilityCategory = request.VulnerabilityCategory;
        participant.SafeguardingCategory = request.SafeguardingCategory;
        participant.ConsentOnFile = request.ConsentOnFile;
        participant.Status = request.Status;
        participant.Remarks = request.Remarks;
        participant.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateParticipantResponse(
            participant.Id, participant.FullName, participant.ParticipantType, participant.BeneficiaryType, participant.Gender, participant.Status, participant.IsActive));
    }
}
