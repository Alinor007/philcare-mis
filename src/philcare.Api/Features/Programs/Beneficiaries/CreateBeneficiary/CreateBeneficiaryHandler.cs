using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Features.Programs.Beneficiaries.CreateBeneficiary;

public sealed class CreateBeneficiaryHandler(AppDbContext db)
{
    public async Task<Result<CreateBeneficiaryResponse>> HandleAsync(CreateBeneficiaryRequest request, CancellationToken cancellationToken)
    {
        if (!request.ConsentOnFile)
        {
            return Result.Failure<CreateBeneficiaryResponse>(
                Error.Validation("Beneficiaries.ConsentRequired", "Consent must be on file before a beneficiary can be registered."));
        }

        // Double-entry gate. Name alone is never enough — distinct people genuinely share names in
        // the communities this serves — so a match also needs a corroborating identifier (same
        // barangay or same phone) before it is worth interrupting the officer over. Soft: the
        // officer can confirm and proceed, which is why this is not a unique index.
        if (!request.ConfirmDuplicate)
        {
            var name = request.FullName.Trim();
            var barangay = request.Barangay?.Trim();
            var phone = request.Phone?.Trim();

            var duplicate = await db.Beneficiaries
                .Where(b => b.IsActive
                    && b.FullName == name
                    && ((barangay != null && b.Barangay == barangay) || (phone != null && b.Phone == phone)))
                .OrderBy(b => b.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (duplicate is not null)
            {
                var matchedOn = !string.IsNullOrWhiteSpace(barangay) && duplicate.Barangay == barangay
                    ? $"barangay {duplicate.Barangay}"
                    : $"phone {duplicate.Phone}";

                return Result.Failure<CreateBeneficiaryResponse>(
                    Error.Conflict("Beneficiaries.PossibleDuplicate",
                        $"{duplicate.FullName} is already registered with the same {matchedOn} "
                        + $"(beneficiary #{duplicate.Id}). Confirm to register this as a different person."));
            }
        }

        // Elevated safeguarding risk does not block registration — it saves the record and
        // surfaces a warning the officer must act on (see Volunteers' orientation gate for the
        // analogous check on the activity-enrollment side).
        var hasSafeguardingRisk = !string.IsNullOrWhiteSpace(request.SafeguardingCategory)
            && !string.Equals(request.SafeguardingCategory, "NONE", StringComparison.OrdinalIgnoreCase);

        var beneficiary = new Beneficiary
        {
            FullName = request.FullName,
            BeneficiaryType = request.BeneficiaryType,
            Gender = request.Gender,
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
            PhotoUrl = request.PhotoUrl,
            IsActive = true
        };

        db.Beneficiaries.Add(beneficiary);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateBeneficiaryResponse(
            beneficiary.Id, beneficiary.FullName, beneficiary.BeneficiaryType, beneficiary.Gender,
            beneficiary.VulnerabilityCategory, beneficiary.SafeguardingCategory, beneficiary.ConsentOnFile,
            beneficiary.Status, beneficiary.IsActive, hasSafeguardingRisk,
            hasSafeguardingRisk ? "Elevated safeguarding risk — officer must be notified." : null));
    }
}
