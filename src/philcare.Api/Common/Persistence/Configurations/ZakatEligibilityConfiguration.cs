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
        // Column stays BeneficiaryId — see SponsorshipConfiguration for the reasoning.
        builder.Property(z => z.BeneficiaryId).HasColumnName("BeneficiaryId");

        builder.Property(z => z.AssessedBy).HasMaxLength(100);
        builder.Property(z => z.AssessmentNotes).HasMaxLength(2000);

        builder.Property(z => z.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(z => z.DecidedBy).HasMaxLength(100);
        builder.Property(z => z.RejectionReason).HasMaxLength(1000);
        builder.Property(z => z.Notes).HasMaxLength(1000);

        builder.HasIndex(z => new { z.BeneficiaryId, z.Status });

        // At most one live (Approved, unexpired) case per beneficiary — NULLs don't collide in a
        // unique index, so only one IsLiveApproval = true row can exist per BeneficiaryId.
        builder.HasIndex(z => new { z.BeneficiaryId, z.IsLiveApproval }).IsUnique();

        builder.HasOne(z => z.Beneficiary)
            .WithMany()
            .HasForeignKey(z => z.BeneficiaryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
