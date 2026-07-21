using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Features.Finance.Donations.CreateDonation;

public sealed class CreateDonationHandler(AppDbContext db)
{
    public async Task<Result<CreateDonationResponse>> HandleAsync(CreateDonationRequest request, CancellationToken cancellationToken)
    {
        var donor = await db.Donors.FirstOrDefaultAsync(d => d.Id == request.DonorId, cancellationToken);

        if (donor is null)
        {
            return Result.Failure<CreateDonationResponse>(Error.NotFound("Donations.DonorNotFound", "Donor not found."));
        }

        if (!donor.IsActive)
        {
            return Result.Failure<CreateDonationResponse>(
                Error.Validation("Donations.DonorInactive", "Cannot record a donation for an inactive donor."));
        }

        var fund = await db.Funds.FirstOrDefaultAsync(f => f.Code == request.FundCode, cancellationToken);

        if (fund is null)
        {
            return Result.Failure<CreateDonationResponse>(Error.NotFound("Donations.FundNotFound", "Fund not found."));
        }

        var buckets = await db.FundingBuckets.Where(b => b.FundCode == fund.Code).ToListAsync(cancellationToken);
        var programBucket = buckets.FirstOrDefault(b => b.BucketType == BucketType.Program) ?? buckets.FirstOrDefault();
        var adminBucket = buckets.FirstOrDefault(b => b.BucketType == BucketType.Admin);

        if (programBucket is null)
        {
            return Result.Failure<CreateDonationResponse>(
                Error.Failure("Donations.NoBucketConfigured", "This fund has no funding bucket configured."));
        }

        if (request.AdminAllowed && adminBucket is null)
        {
            return Result.Failure<CreateDonationResponse>(
                Error.Validation("Donations.NoAdminBucket", "This fund has no admin/amil bucket configured; admin allocation is not allowed."));
        }

        var adminRateCap = adminBucket?.MaxAdminRate ?? 0m;

        if (request.AdminRateInput > adminRateCap)
        {
            return Result.Failure<CreateDonationResponse>(
                Error.Validation("Donations.AdminRateExceeded", $"Admin rate cannot exceed the bucket cap of {adminRateCap:P1}."));
        }

        var amountPhp = Math.Round(request.AmountOriginal * request.FxRateToPhp, 2);
        var adminRateApplied = request.AdminAllowed ? request.AdminRateInput : 0m;
        var adminAmount = Math.Round(amountPhp * adminRateApplied, 2);
        var programAmount = amountPhp - adminAmount;

        var donation = new Donation
        {
            DonorId = donor.Id,
            AmountOriginal = request.AmountOriginal,
            Currency = request.Currency,
            FxRateToPhp = request.FxRateToPhp,
            AmountPhp = amountPhp,
            DateReceived = request.DateReceived,
            Channel = request.Channel,
            Purpose = request.Purpose,
            RestrictedFlag = request.RestrictedFlag,
            ProgramOrProject = request.ProgramOrProject,
            FundCode = fund.Code,
            ReceiptNo = request.ReceiptNo,
            CashDocumentationStatus = request.CashDocumentationStatus,
            SourceVerified = request.SourceVerified,
            KydStatus = donor.KydStatus,
            RiskRating = donor.RiskRating,
            AdminAllowed = request.AdminAllowed,
            AdminRateInput = request.AdminRateInput,
            AdminRateCap = adminRateCap,
            AdminRateApplied = adminRateApplied,
            ProgramAllocationPhp = programAmount,
            AdminAllocationPhp = adminAmount,
            ProgramBucketCode = programBucket.Code,
            AdminBucketCode = adminAmount > 0 ? adminBucket!.Code : null,
            AllocationStatus = "OK",
            Notes = request.Notes,
            IsVoided = false
        };

        db.Donations.Add(donation);

        var isZakat = string.Equals(fund.Code, FinanceRules.ZakatFundCode, StringComparison.OrdinalIgnoreCase);
        var reportingYear = request.DateReceived.Year;

        var programAllocation = new Allocation
        {
            AllocationDate = request.DateReceived,
            Donation = donation,
            SourceFundCode = fund.Code,
            GrossAmountPhp = amountPhp,
            AllocationType = AllocationType.Program,
            AllocationRate = amountPhp == 0 ? 0 : Math.Round(programAmount / amountPhp, 4),
            AllocatedAmountPhp = programAmount,
            TargetBucketCode = programBucket.Code,
            ReportingYear = reportingYear,
            PolicyCap = 0,
            Status = "OK"
        };

        db.Allocations.Add(programAllocation);
        programBucket.AllocatedAmount += programAmount;

        if (adminAmount > 0)
        {
            var adminAllocation = new Allocation
            {
                AllocationDate = request.DateReceived,
                Donation = donation,
                SourceFundCode = fund.Code,
                GrossAmountPhp = amountPhp,
                AllocationType = isZakat ? AllocationType.Amil : AllocationType.Admin,
                AllocationRate = adminRateApplied,
                AllocatedAmountPhp = adminAmount,
                TargetBucketCode = adminBucket!.Code,
                ReportingYear = reportingYear,
                PolicyCap = adminRateCap,
                Status = "OK"
            };

            db.Allocations.Add(adminAllocation);
            adminBucket.AllocatedAmount += adminAmount;
        }

        await db.SaveChangesAsync(cancellationToken);

        var allocationLines = donation.Allocations
            .Select(a => new AllocationLineResponse(a.AllocationType, a.TargetBucketCode, a.AllocationRate, a.AllocatedAmountPhp))
            .ToList();

        return Result.Success(new CreateDonationResponse(
            donation.Id, donation.DonorId, donation.AmountOriginal, donation.Currency, donation.FxRateToPhp, donation.AmountPhp,
            donation.DateReceived, donation.Channel, donation.Purpose, donation.RestrictedFlag, donation.ProgramOrProject,
            donation.FundCode, donation.ReceiptNo, donation.AdminAllowed, donation.AdminRateInput, donation.AdminRateCap,
            donation.AdminRateApplied, donation.ProgramAllocationPhp, donation.AdminAllocationPhp, donation.ProgramBucketCode,
            donation.AdminBucketCode, donation.AllocationStatus, donation.IsVoided, allocationLines));
    }
}
