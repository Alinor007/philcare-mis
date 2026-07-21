using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Finance.Donors.UpdateDonor;

public sealed class UpdateDonorHandler(AppDbContext db)
{
    public async Task<Result<UpdateDonorResponse>> HandleAsync(int id, UpdateDonorRequest request, CancellationToken cancellationToken)
    {
        var donor = await db.Donors.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (donor is null)
        {
            return Result.Failure<UpdateDonorResponse>(Error.NotFound("Donors.NotFound", "Donor not found."));
        }

        donor.Name = request.Name;
        donor.Type = request.Type;
        donor.Email = request.Email;
        donor.Phone = request.Phone;
        donor.Address = request.Address;
        donor.Country = request.Country;
        donor.Notes = request.Notes;
        donor.IsActive = request.IsActive;
        donor.KydStatus = request.KydStatus;
        donor.RiskRating = request.RiskRating;
        donor.PepFlag = request.PepFlag;
        donor.PrivacyConsent = request.PrivacyConsent;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateDonorResponse(
            donor.Id, donor.Name, donor.Type, donor.Email, donor.Phone, donor.Address, donor.Country, donor.Notes,
            donor.IsActive, donor.KydStatus, donor.RiskRating, donor.PepFlag, donor.PrivacyConsent));
    }
}
