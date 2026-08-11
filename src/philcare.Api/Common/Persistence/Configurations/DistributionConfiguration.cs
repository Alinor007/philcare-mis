using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Programs.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class DistributionConfiguration : IEntityTypeConfiguration<Distribution>
{
    public void Configure(EntityTypeBuilder<Distribution> builder)
    {
        builder.ToTable("Distributions");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DistributionType).IsRequired().HasMaxLength(50);
        builder.Property(d => d.FundingBucketCode).HasMaxLength(20);
        builder.Property(d => d.UnitValuePhp).HasPrecision(14, 2).HasDefaultValue(0m);
        builder.Property(d => d.TotalValuePhp).HasPrecision(14, 2);
        builder.Property(d => d.BeneficiaryCount).HasDefaultValue(1);
        builder.Property(d => d.Location).HasMaxLength(200);
        builder.Property(d => d.ProcessedBy).HasMaxLength(200);
        builder.Property(d => d.ZakatAsnaf).HasMaxLength(50);
        builder.Property(d => d.Notes).HasMaxLength(1000);

        builder.Property(d => d.IsVoided).HasDefaultValue(false);

        builder.HasIndex(d => d.FundingBucketCode);
        builder.HasIndex(d => d.DistributionDate);
        builder.HasIndex(d => d.ExpenseId);

        builder.HasOne(d => d.Beneficiary)
            .WithMany(p => p.Distributions)
            .HasForeignKey(d => d.BeneficiaryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Activity)
            .WithMany(a => a.Distributions)
            .HasForeignKey(d => d.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Nullable forever (see Distribution's class doc) — historical and zero-value rows have
        // no linked Expense. Restrict, not Cascade: an Expense is never deleted, only voided.
        builder.HasOne(d => d.Expense)
            .WithMany()
            .HasForeignKey(d => d.ExpenseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
