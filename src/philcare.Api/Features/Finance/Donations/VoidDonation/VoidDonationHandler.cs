using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using philcare.Api.Common.Domain;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Donations.Emails;
using philcare.Api.Features.Finance.Domain;
using philcare.Api.Infrastructure.Email;

namespace philcare.Api.Features.Finance.Donations.VoidDonation;

public sealed class VoidDonationHandler(AppDbContext db, IOptions<EmailOptions> emailOptions)
{
    public async Task<Result> HandleAsync(int id, CancellationToken cancellationToken)
    {
        var donation = await db.Donations
            .Include(d => d.Donor)
            .Include(d => d.Allocations)
            .ThenInclude(a => a.TargetBucket)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (donation is null)
        {
            return Result.Failure(Error.NotFound("Donations.NotFound", "Donation not found."));
        }

        if (donation.IsVoided)
        {
            return Result.Failure(Error.Conflict("Donations.AlreadyVoided", "This donation has already been voided."));
        }

        foreach (var allocation in donation.Allocations)
        {
            var bucket = allocation.TargetBucket;
            var remainingAfterVoid = bucket.AllocatedAmount - allocation.AllocatedAmountPhp;

            if (remainingAfterVoid < bucket.ExpensedAmount)
            {
                return Result.Failure(Error.Validation(
                    "Donations.FundsAlreadySpent",
                    $"Cannot void this donation because bucket '{bucket.Code}' has already spent funds from this allocation."));
            }
        }

        foreach (var allocation in donation.Allocations)
        {
            allocation.TargetBucket.AllocatedAmount -= allocation.AllocatedAmountPhp;
        }

        donation.IsVoided = true;

        // Same outbox pattern as CreateDonationHandler — enqueued in the same SaveChangesAsync as
        // the void itself, so the notification can never be lost even if the process dies right
        // after this line.
        if (string.IsNullOrWhiteSpace(donation.Donor.Email))
        {
            db.OutboxEmails.Add(new OutboxEmail
            {
                Donation = donation,
                EmailType = EmailType.DonationVoided,
                ToEmail = string.Empty,
                ToName = donation.Donor.Name,
                Subject = "(skipped — no donor email on file)",
                HtmlBody = string.Empty,
                Status = EmailDeliveryStatus.Skipped,
                LastError = "Donor has no email address on file."
            });
        }
        else
        {
            var (subject, html, text) = DonationEmailComposer.ComposeVoided(donation, donation.Donor, emailOptions.Value);
            db.OutboxEmails.Add(new OutboxEmail
            {
                Donation = donation,
                EmailType = EmailType.DonationVoided,
                ToEmail = donation.Donor.Email,
                ToName = donation.Donor.Name,
                Subject = subject,
                HtmlBody = html,
                TextBody = text,
                Status = EmailDeliveryStatus.Pending
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
