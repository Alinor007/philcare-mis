using philcare.Api.Common.Domain;

namespace philcare.Api.Features.Finance.Domain;

public enum EmailType
{
    DonationConfirmation,
    DonationVoided
}

public enum EmailDeliveryStatus
{
    Pending,
    Sent,
    Failed,
    Skipped,
    Abandoned
}

/// <summary>
/// Transactional outbox row for a single email send attempt. Written in the SAME SaveChangesAsync
/// call as the donation it concerns — see CreateDonationHandler / VoidDonationHandler — so the
/// intent to notify a donor can never be lost even if the process dies before OutboxDispatcher
/// gets to it. IdempotencyKey is passed to Resend on every attempt so a retry (ours or a second
/// dispatcher instance) can never result in a duplicate send.
/// </summary>
public class OutboxEmail : Entity
{
    public Guid IdempotencyKey { get; set; } = Guid.NewGuid();

    public int? DonationId { get; set; }
    public Donation? Donation { get; set; }

    public EmailType EmailType { get; set; }

    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }

    public EmailDeliveryStatus Status { get; set; } = EmailDeliveryStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ProviderMessageId { get; set; }
}
