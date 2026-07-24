using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Sponsorships.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class SponsorshipConfiguration : IEntityTypeConfiguration<Sponsorship>
{
    public void Configure(EntityTypeBuilder<Sponsorship> builder)
    {
        builder.ToTable("Sponsorships");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SponsorshipType).IsRequired().HasMaxLength(50);
        builder.Property(s => s.MonthlyAmountPhp).HasPrecision(14, 2);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.CaseWorker).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.HasIndex(s => new { s.DonorId, s.ParticipantId });

        builder.HasOne(s => s.Donor)
            .WithMany()
            .HasForeignKey(s => s.DonorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Participant)
            .WithMany()
            .HasForeignKey(s => s.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
