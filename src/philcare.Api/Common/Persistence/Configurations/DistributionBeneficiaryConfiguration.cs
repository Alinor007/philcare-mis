using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class DistributionBeneficiaryConfiguration : IEntityTypeConfiguration<DistributionBeneficiary>
{
    public void Configure(EntityTypeBuilder<DistributionBeneficiary> builder)
    {
        builder.ToTable("DistributionBeneficiaries");

        builder.HasKey(db => db.Id);

        builder.Property(db => db.EvidenceLink).HasMaxLength(500);
        builder.Property(db => db.Remarks).HasMaxLength(1000);
        builder.Property(db => db.ReceivedConfirmation).HasDefaultValue(false);
        builder.Property(db => db.IsActive).HasDefaultValue(true);

        // One roster row per beneficiary per distribution; soft-delete means re-adding a removed
        // beneficiary reactivates the existing row rather than inserting a duplicate.
        builder.HasIndex(db => new { db.DistributionId, db.BeneficiaryId }).IsUnique();

        // Reach reports pivot on the beneficiary, not the distribution.
        builder.HasIndex(db => db.BeneficiaryId);

        builder.HasOne(db => db.Distribution)
            .WithMany(d => d.Beneficiaries)
            .HasForeignKey(db => db.DistributionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(db => db.Beneficiary)
            .WithMany(b => b.DistributionBeneficiaries)
            .HasForeignKey(db => db.BeneficiaryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
