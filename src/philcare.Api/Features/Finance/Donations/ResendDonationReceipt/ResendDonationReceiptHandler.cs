using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Donations.Emails;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Infrastructure.Email;

namespace philcare.Api.Features.Finance.Donations.ResendDonationReceipt;

public sealed record ResendDonationReceiptResponse(string Status, string ToEmail, DateTime QueuedAt);

/// <summary>
/// Manual re-send, e.g. the original delivery failed/was abandoned, or the officer fixed the
/// donor's email on file after the fact. Always enqueues a NEW OutboxEmail row with a NEW
/// IdempotencyKey — an officer asking for a re-send genuinely wants another delivery, so this must
/// not be deduplicated against the original confirmation.
/// </summary>
public sealed class ResendDonationReceiptHandler(AppDbContext db, IOptions<EmailOptions> emailOptions)
{
    public async Task<Result<ResendDonationReceiptResponse>> HandleAsync(int donationId, CancellationToken cancellationToken)
    {
        var donation = await db.Donations
            .Include(d => d.Donor)
            .Include(d => d.Fund)
            .FirstOrDefaultAsync(d => d.Id == donationId, cancellationToken);

        if (donation is null)
        {
            return Result.Failure<ResendDonationReceiptResponse>(Error.NotFound("Donations.NotFound", "Donation not found."));
        }

        if (donation.IsVoided)
        {
            return Result.Failure<ResendDonationReceiptResponse>(
                Error.Validation("Donations.Voided", "Cannot re-send a receipt for a voided donation."));
        }

        if (string.IsNullOrWhiteSpace(donation.Donor.Email))
        {
            return Result.Failure<ResendDonationReceiptResponse>(
                Error.Validation("Donations.DonorHasNoEmail", "This donor has no email address on file."));
        }

        var (subject, html, text) = DonationEmailComposer.ComposeConfirmation(donation, donation.Donor, donation.Fund, emailOptions.Value);

        var outboxEmail = new OutboxEmail
        {
            Donation = donation,
            EmailType = EmailType.DonationConfirmation,
            ToEmail = donation.Donor.Email,
            ToName = donation.Donor.Name,
            Subject = subject,
            HtmlBody = html,
            TextBody = text,
            Status = EmailDeliveryStatus.Pending
        };

        db.OutboxEmails.Add(outboxEmail);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new ResendDonationReceiptResponse("Pending", outboxEmail.ToEmail, outboxEmail.CreatedAt));
    }
}
