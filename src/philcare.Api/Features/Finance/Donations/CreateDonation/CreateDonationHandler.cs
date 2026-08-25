using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Donations.Emails;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Infrastructure.Email;

namespace philcare.Api.Features.Finance.Donations.CreateDonation;

public sealed class CreateDonationHandler(AppDbContext db, IOptions<EmailOptions> emailOptions)
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
            // Server-assigned below, before Add() — never client-supplied, same rule already
            // governing AmountPhp/TotalValuePhp.
            ReceiptNo = await NextReceiptNoAsync(db, request.DateReceived.Year, cancellationToken),
            TransactionRef = request.TransactionRef,
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

        // Enqueue the confirmation email in the SAME SaveChangesAsync as the donation itself — see
        // OutboxEmail's doc comment. A donation is never blocked or failed by this: composing the
        // email cannot throw (all inputs are already-validated strings), and the row's own delivery
        // is handled asynchronously by OutboxDispatcher, not here.
        if (string.IsNullOrWhiteSpace(donor.Email))
        {
            db.OutboxEmails.Add(new OutboxEmail
            {
                Donation = donation,
                EmailType = EmailType.DonationConfirmation,
                ToEmail = string.Empty,
                ToName = donor.Name,
                Subject = "(skipped — no donor email on file)",
                HtmlBody = string.Empty,
                Status = EmailDeliveryStatus.Skipped,
                LastError = "Donor has no email address on file."
            });
        }
        else
        {
            var (subject, html, text) = DonationEmailComposer.ComposeConfirmation(donation, donor, fund, emailOptions.Value);
            db.OutboxEmails.Add(new OutboxEmail
            {
                Donation = donation,
                EmailType = EmailType.DonationConfirmation,
                ToEmail = donor.Email,
                ToName = donor.Name,
                Subject = subject,
                HtmlBody = html,
                TextBody = text,
                Status = EmailDeliveryStatus.Pending
            });
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique index on ReceiptNo caught two donations racing for the same number — the
            // count-based generator above is check-then-act. Rare (would need two donations saved
            // in the same instant) and not worth an automatic retry given the save already carries
            // a partially-built Allocation graph; asking the officer to resubmit is simpler and safer.
            return Result.Failure<CreateDonationResponse>(
                Error.Conflict("Donations.ReceiptNumberConflict", "Receipt number assignment conflicted with another donation saved at the same time. Please try again."));
        }

        var allocationLines = donation.Allocations
            .Select(a => new AllocationLineResponse(a.AllocationType, a.TargetBucketCode, a.AllocationRate, a.AllocatedAmountPhp))
            .ToList();

        return Result.Success(new CreateDonationResponse(
            donation.Id, donation.DonorId, donation.AmountOriginal, donation.Currency, donation.FxRateToPhp, donation.AmountPhp,
            donation.DateReceived, donation.Channel, donation.Purpose, donation.RestrictedFlag, donation.ProgramOrProject,
            donation.FundCode, donation.ReceiptNo, donation.TransactionRef, donation.AdminAllowed, donation.AdminRateInput, donation.AdminRateCap,
            donation.AdminRateApplied, donation.ProgramAllocationPhp, donation.AdminAllocationPhp, donation.ProgramBucketCode,
            donation.AdminBucketCode, donation.AllocationStatus, donation.IsVoided, allocationLines));
    }

    /// <summary>
    /// "DON-2026-0001" style, resetting per calendar year. Not a DB sequence — MariaDB
    /// auto-increment can't reset yearly or carry a prefix — so this counts existing receipts for
    /// the year instead. That makes it check-then-act; see the DbUpdateException handling above for
    /// how a collision is surfaced.
    /// </summary>
    private static async Task<string> NextReceiptNoAsync(AppDbContext db, int year, CancellationToken cancellationToken)
    {
        var yearPrefix = $"DON-{year}-";
        var count = await db.Donations
            .Where(d => d.ReceiptNo != null && d.ReceiptNo.StartsWith(yearPrefix))
            .CountAsync(cancellationToken);
        return $"{yearPrefix}{count + 1:D4}";
    }
}
