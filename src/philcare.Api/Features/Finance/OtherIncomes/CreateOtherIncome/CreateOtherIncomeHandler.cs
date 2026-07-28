using Microsoft.EntityFrameworkCore;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Features.ReferenceData.Domain;

namespace philcare.Api.Features.Finance.OtherIncomes.CreateOtherIncome;

public sealed class CreateOtherIncomeHandler(AppDbContext db)
{
    public async Task<Result<CreateOtherIncomeResponse>> HandleAsync(CreateOtherIncomeRequest request, CancellationToken cancellationToken)
    {
        var bucket = await db.FundingBuckets.FirstOrDefaultAsync(b => b.Code == request.FundingBucketCode, cancellationToken);

        if (bucket is null)
        {
            return Result.Failure<CreateOtherIncomeResponse>(Error.NotFound("OtherIncome.FundingBucketNotFound", "Funding bucket not found."));
        }

        // Zakat corpus may only enter through zakat donations (sharia segregation) — this also
        // protects GetZakatAmilReportEndpoint, whose 12.5% cap check compares a donation-only
        // total against the ZAK-AMIL bucket balance; a non-donation inflow into ZAKA-FUND would
        // falsely appear to breach the cap.
        if (string.Equals(bucket.FundCode, FinanceRules.ZakatFundCode, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CreateOtherIncomeResponse>(
                Error.Validation("OtherIncome.ZakatFundNotAllowed",
                    "Non-donation income cannot be recorded into zakat fund buckets; zakat money may only enter via zakat donations."));
        }

        var validIncomeType = await db.LookupItems.AnyAsync(
            l => l.Category == LookupCategory.IncomeType && l.Code == request.IncomeType && l.IsActive, cancellationToken);

        if (!validIncomeType)
        {
            return Result.Failure<CreateOtherIncomeResponse>(
                Error.Validation("OtherIncome.InvalidIncomeType", "Income type must be a valid income_type lookup code."));
        }

        var amountPhp = Math.Round(request.AmountOriginal * request.FxRateToPhp, 2);

        var income = new OtherIncome
        {
            IncomeType = request.IncomeType,
            Source = request.Source,
            DateReceived = request.DateReceived,
            ReportingYear = request.DateReceived.Year,
            AmountOriginal = request.AmountOriginal,
            Currency = request.Currency,
            FxRateToPhp = request.FxRateToPhp,
            AmountPhp = amountPhp,
            FundCode = bucket.FundCode,
            FundingBucketCode = bucket.Code,
            ReceiptNo = request.ReceiptNo,
            EvidenceLink = request.EvidenceLink,
            Notes = request.Notes,
            IsVoided = false
        };

        db.OtherIncomes.Add(income);

        var allocation = new Allocation
        {
            AllocationDate = request.DateReceived,
            OtherIncome = income,
            SourceFundCode = bucket.FundCode,
            GrossAmountPhp = amountPhp,
            AllocationType = AllocationType.Income,
            AllocationRate = 1m,
            AllocatedAmountPhp = amountPhp,
            TargetBucketCode = bucket.Code,
            ReportingYear = income.ReportingYear,
            PolicyCap = 0,
            Status = "OK",
            EvidenceNotes = request.Source
        };

        db.Allocations.Add(allocation);
        bucket.AllocatedAmount += amountPhp;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateOtherIncomeResponse(
            income.Id, income.IncomeType, income.Source, income.AmountOriginal, income.Currency, income.FxRateToPhp,
            income.AmountPhp, income.DateReceived, income.FundCode, income.FundingBucketCode, income.ReceiptNo,
            income.EvidenceLink, income.Notes, income.IsVoided));
    }
}
