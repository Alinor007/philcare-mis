using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class OutboxEmailConfiguration : IEntityTypeConfiguration<OutboxEmail>
{
    public void Configure(EntityTypeBuilder<OutboxEmail> builder)
    {
        builder.ToTable("OutboxEmails");

        builder.HasKey(e => e.Id);

        // Passed to Resend as the send-idempotency key — must be unique so a retry can never be
        // mistaken for a fresh row, and so two dispatcher instances can't double-submit.
        builder.HasIndex(e => e.IdempotencyKey).IsUnique();

        // What OutboxDispatcher polls: due rows not yet resolved.
        builder.HasIndex(e => new { e.Status, e.NextAttemptAt });
        builder.HasIndex(e => e.DonationId);

        builder.Property(e => e.EmailType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(e => e.ToEmail).IsRequired().HasMaxLength(256);
        builder.Property(e => e.ToName).HasMaxLength(200);
        builder.Property(e => e.Subject).IsRequired().HasMaxLength(300);
        builder.Property(e => e.HtmlBody).IsRequired().HasColumnType("MEDIUMTEXT");
        builder.Property(e => e.TextBody).HasColumnType("TEXT");
        builder.Property(e => e.LastError).HasMaxLength(1000);
        builder.Property(e => e.ProviderMessageId).HasMaxLength(100);

        builder.HasOne(e => e.Donation)
            .WithMany(d => d.OutboxEmails)
            .HasForeignKey(e => e.DonationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
