using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using philcare.Api.Common.Persistence;
using philcare.Api.Features.Finance.Domain;
using Resend;

namespace philcare.Api.Infrastructure.Email;

/// <summary>
/// Polls OutboxEmails for due Pending rows and sends them via Resend. Runs independently of any
/// HTTP request — CreateDonationHandler/VoidDonationHandler only ever write a Pending row in the
/// same SaveChangesAsync as the donation itself, so a donation is never blocked or failed by email
/// delivery, and a row written just before a crash is still on disk for this loop to pick up on
/// restart.
///
/// Each tick opens its own DI scope (AppDbContext is scoped) and is wrapped in a top-level
/// try/catch so one bad tick can never take the host down. Every send passes the row's stable
/// IdempotencyKey to Resend, so a retry after a timeout — or a second instance of this dispatcher —
/// can never result in a duplicate email reaching the donor.
/// </summary>
public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled || string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            logger.LogWarning(
                "Email sending is disabled (Email:Enabled=false or Email:ApiKey is blank). " +
                "OutboxDispatcher will idle; queued rows stay Pending and will drain once enabled.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.Value.Enabled && !string.IsNullOrWhiteSpace(options.Value.ApiKey))
                {
                    await ProcessBatchAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxDispatcher tick failed unexpectedly; will retry next tick.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.Value.PollIntervalSeconds)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var resend = scope.ServiceProvider.GetRequiredService<IResend>();

        var now = DateTime.UtcNow;
        var due = await db.OutboxEmails
            .Where(e => e.Status == EmailDeliveryStatus.Pending && (e.NextAttemptAt == null || e.NextAttemptAt <= now))
            .OrderBy(e => e.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var email in due)
        {
            await SendOneAsync(db, resend, email, ct);
        }
    }

    private async Task SendOneAsync(AppDbContext db, IResend resend, OutboxEmail email, CancellationToken ct)
    {
        email.AttemptCount++;

        try
        {
            var message = new EmailMessage
            {
                From = options.Value.FromAddress,
                To = email.ToEmail,
                Subject = email.Subject,
                HtmlBody = email.HtmlBody,
                TextBody = email.TextBody,
            };

            if (!string.IsNullOrWhiteSpace(options.Value.ReplyToAddress))
            {
                message.ReplyTo = options.Value.ReplyToAddress;
            }

            var response = await resend.EmailSendAsync(email.IdempotencyKey.ToString(), message, ct);

            email.Status = EmailDeliveryStatus.Sent;
            email.SentAt = DateTime.UtcNow;
            email.ProviderMessageId = response.Content.ToString();
            email.LastError = null;
            email.NextAttemptAt = null;
        }
        catch (Exception ex)
        {
            email.LastError = ex.Message;

            if (email.AttemptCount >= options.Value.MaxAttempts)
            {
                email.Status = EmailDeliveryStatus.Abandoned;
                email.NextAttemptAt = null;
                logger.LogError(ex, "Giving up on OutboxEmail {EmailId} (donation {DonationId}) after {Attempts} attempts.",
                    email.Id, email.DonationId, email.AttemptCount);
            }
            else
            {
                email.Status = EmailDeliveryStatus.Pending;
                email.NextAttemptAt = DateTime.UtcNow.AddSeconds(BackoffSeconds(email.AttemptCount));
                logger.LogWarning(ex, "OutboxEmail {EmailId} (donation {DonationId}) attempt {Attempt} failed; retrying at {NextAttemptAt}.",
                    email.Id, email.DonationId, email.AttemptCount, email.NextAttemptAt);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Exponential backoff: 30s, 60s, 120s, 240s, capped at 5 minutes.</summary>
    private static double BackoffSeconds(int attempt) => Math.Min(300, Math.Pow(2, attempt) * 15);
}
