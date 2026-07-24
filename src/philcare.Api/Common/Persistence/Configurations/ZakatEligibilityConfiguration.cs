using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Zakat.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class ZakatEligibilityConfiguration : IEntityTypeConfiguration<ZakatEligibility>
{
    public void Configure(EntityTypeBuilder<ZakatEligibility> builder)
    {
        builder.ToTable("ZakatEligibilities");

        builder.HasKey(z => z.Id);

        builder.Property(z => z.AsnafCategory).IsRequired().HasMaxLength(50);
        builder.Property(z => z.MonthlyIncomePhp).HasPrecision(14, 2);
        builder.Property(z => z.AssessedBy).HasMaxLength(100);
        builder.Property(z => z.AssessmentNotes).HasMaxLength(2000);

        builder.Property(z => z.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(z => z.DecidedBy).HasMaxLength(100);
        builder.Property(z => z.RejectionReason).HasMaxLength(1000);
        builder.Property(z => z.Notes).HasMaxLength(1000);

        builder.HasIndex(z => new { z.ParticipantId, z.Status });

        builder.HasOne(z => z.Participant)
            .WithMany()
            .HasForeignKey(z => z.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
