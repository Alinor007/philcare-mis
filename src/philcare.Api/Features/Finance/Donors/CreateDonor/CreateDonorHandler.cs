using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Donors.CreateDonor;

public sealed class CreateDonorHandler(AppDbContext db)
{
    public async Task<Result<CreateDonorResponse>> HandleAsync(CreateDonorRequest request, CancellationToken cancellationToken)
    {
        var donor = new Donor
        {
            Name = request.Name,
            Type = request.Type,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            Country = request.Country,
            Notes = request.Notes,
            IsActive = true,
            KydStatus = KydStatus.Pending,
            RiskRating = request.RiskRating,
            PepFlag = request.PepFlag,
            PrivacyConsent = request.PrivacyConsent
        };

        db.Donors.Add(donor);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateDonorResponse(
            donor.Id, donor.Name, donor.Type, donor.Email, donor.Phone, donor.Address, donor.Country, donor.Notes,
            donor.IsActive, donor.KydStatus, donor.RiskRating, donor.PepFlag, donor.PrivacyConsent));
    }
}
