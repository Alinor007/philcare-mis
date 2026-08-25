using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;

namespace philcare.Api.Features.Programs.DistributionBeneficiaries.UpdateDistributionBeneficiary;

/// <summary>
/// Corrects a roster entry (receipt confirmation, evidence, remarks) in place, so recording a
/// signature after the fact does not require DELETE + re-POST and destroy the audit trail.
/// Reach is untouched here: this edits an already-active row, so the count cannot change.
/// </summary>
public sealed class UpdateDistributionBeneficiaryHandler(AppDbContext db)
{
    public async Task<Result<UpdateDistributionBeneficiaryResponse>> HandleAsync(
        int distributionId, int beneficiaryId, UpdateDistributionBeneficiaryRequest request, CancellationToken cancellationToken)
    {
        var row = await db.DistributionBeneficiaries
            .Include(r => r.Beneficiary)
            .FirstOrDefaultAsync(
                r => r.DistributionId == distributionId && r.BeneficiaryId == beneficiaryId && r.IsActive, cancellationToken);

        if (row is null)
        {
            return Result.Failure<UpdateDistributionBeneficiaryResponse>(
                Error.NotFound("DistributionBeneficiaries.NotFound", "This beneficiary is not on this distribution's roster."));
        }

        row.ReceivedConfirmation = request.ReceivedConfirmation;
        row.EvidenceLink = request.EvidenceLink;
        row.Remarks = request.Remarks;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new UpdateDistributionBeneficiaryResponse(
            row.Id, row.DistributionId, row.BeneficiaryId, row.Beneficiary.FullName, row.ReceivedConfirmation));
    }
}
