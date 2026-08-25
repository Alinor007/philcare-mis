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

        // The property is BeneficiaryId; the column stays BeneficiaryId. Renaming a column is the
        // MariaDB 10.4 operation this project cannot perform, and the rows already point at the
        // right records — only the C# name changed.
        builder.Property(s => s.BeneficiaryId).HasColumnName("BeneficiaryId");

        builder.Property(s => s.CaseWorker).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        // At most one live (Active or Paused) pledge per donor/beneficiary pair — NULLs don't
        // collide in a unique index, so only one IsActiveSponsorship = true row can exist per pair.
        // Its (DonorId, BeneficiaryId) prefix also serves the lookups the old non-unique index on
        // those two columns covered, so that index is dropped rather than kept as a duplicate.
        builder.HasIndex(s => new { s.DonorId, s.BeneficiaryId, s.IsActiveSponsorship }).IsUnique();

        builder.HasOne(s => s.Donor)
            .WithMany()
            .HasForeignKey(s => s.DonorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Beneficiary)
            .WithMany()
            .HasForeignKey(s => s.BeneficiaryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
