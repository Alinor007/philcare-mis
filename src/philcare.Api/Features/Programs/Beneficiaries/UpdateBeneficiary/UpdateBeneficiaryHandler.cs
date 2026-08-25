using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.Beneficiaries.UpdateBeneficiary;

public sealed class UpdateBeneficiaryHandler(AppDbContext db)
{
    public async Task<Result<UpdateBeneficiaryResponse>> HandleAsync(int id, UpdateBeneficiaryRequest request, CancellationToken cancellationToken)
    {
        var beneficiary = await db.Beneficiaries.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (beneficiary is null)
        {
            return Result.Failure<UpdateBeneficiaryResponse>(Error.NotFound("Beneficiaries.NotFound", "Beneficiary not found."));
        }

        if (!request.ConsentOnFile)
        {
            return Result.Failure<UpdateBeneficiaryResponse>(
                Error.Validation("Beneficiaries.ConsentRequired", "Consent must be on file before a beneficiary can be registered."));
        }

        var hasSafeguardingRisk = !string.IsNullOrWhiteSpace(request.SafeguardingCategory)
            && !string.Equals(request.SafeguardingCategory, "NONE", StringComparison.OrdinalIgnoreCase);

        beneficiary.FullName = request.FullName;
        beneficiary.BeneficiaryType = request.BeneficiaryType;
        beneficiary.Gender = request.Gender;
        beneficiary.Phone = request.Phone;
        beneficiary.Barangay = request.Barangay;
        beneficiary.City = request.City;
        beneficiary.Province = request.Province;
        beneficiary.Region = request.Region;
        beneficiary.Country = request.Country;
        beneficiary.VulnerabilityCategory = request.VulnerabilityCategory;
        beneficiary.SafeguardingCategory = request.SafeguardingCategory;
        beneficiary.ConsentOnFile = request.ConsentOnFile;
        beneficiary.Status = request.Status;
        beneficiary.Remarks = request.Remarks;
        beneficiary.PhotoUrl = request.PhotoUrl;
        beneficiary.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateBeneficiaryResponse(
            beneficiary.Id, beneficiary.FullName, beneficiary.BeneficiaryType, beneficiary.Gender,
            beneficiary.Status, beneficiary.IsActive, hasSafeguardingRisk,
            hasSafeguardingRisk ? "Elevated safeguarding risk — officer must be notified." : null));
    }
}
