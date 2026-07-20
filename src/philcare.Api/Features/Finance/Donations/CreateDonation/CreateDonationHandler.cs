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

        var isZakat = string.Equals(request.FundType, FinanceRules.ZakatFundType, StringComparison.OrdinalIgnoreCase);

        if (request.AdminRate > FinanceRules.MaxAdminRate)
        {
            return Result.Failure<CreateDonationResponse>(
                Error.Validation("Donations.AdminRateExceeded", $"Admin rate cannot exceed {FinanceRules.MaxAdminRate:P1}."));
        }

        if (!isZakat && request.AmilRate > 0)
        {
            return Result.Failure<CreateDonationResponse>(
                Error.Validation("Donations.AmilNotAllowed", "Amil rate is only allowed for zakat donations."));
        }

        if (request.AmilRate > FinanceRules.MaxAmilRate)
        {
            return Result.Failure<CreateDonationResponse>(
                Error.Validation("Donations.AmilRateExceeded", $"Amil rate cannot exceed {FinanceRules.MaxAmilRate:P1}."));
        }

        var adminAmount = request.AdminAllowed ? Math.Round(request.Amount * request.AdminRate, 2) : 0m;
        var amilAmount = isZakat ? Math.Round(request.Amount * request.AmilRate, 2) : 0m;
        var programAmount = request.Amount - adminAmount - amilAmount;

        var donation = new Donation
        {
            DonorId = donor.Id,
            Amount = request.Amount,
            Currency = request.Currency,
            FundType = request.FundType,
            ReceivedDate = request.ReceivedDate,
            PaymentMethod = request.PaymentMethod,
            AdminAllowed = request.AdminAllowed,
            AdminRate = request.AdminRate,
            AmilRate = request.AmilRate,
            Reference = request.Reference,
            Notes = request.Notes,
            IsVoided = false
        };

        db.Donations.Add(donation);

        var bucket = await db.FundBuckets.FirstOrDefaultAsync(b => b.FundType == request.FundType, cancellationToken);

        if (bucket is null)
        {
            bucket = new FundBucket
            {
                Name = $"{request.FundType} Fund",
                FundType = request.FundType
            };
            db.FundBuckets.Add(bucket);
        }

        bucket.TotalReceived += request.Amount;
        bucket.AdminAllocated += adminAmount;
        bucket.ProgramAllocated += programAmount;

        var allocation = new Allocation
        {
            Donation = donation,
            FundBucket = bucket,
            ProgramAmount = programAmount,
            AdminAmount = adminAmount,
            AmilAmount = amilAmount
        };

        db.Allocations.Add(allocation);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateDonationResponse(
            donation.Id, donation.DonorId, donation.Amount, donation.Currency, donation.FundType, donation.ReceivedDate,
            donation.PaymentMethod, donation.AdminAllowed, donation.AdminRate, donation.AmilRate, donation.Reference,
            donation.Notes, donation.IsVoided,
            new AllocationResponse(allocation.ProgramAmount, allocation.AdminAmount, allocation.AmilAmount)));
    }
}
