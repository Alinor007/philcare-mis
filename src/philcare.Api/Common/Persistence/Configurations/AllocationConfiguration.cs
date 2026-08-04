using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.ToTable("Allocations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.SourceFundCode).IsRequired().HasMaxLength(20);
        builder.Property(a => a.TargetBucketCode).IsRequired().HasMaxLength(20);

        builder.Property(a => a.GrossAmountPhp).HasPrecision(14, 2);
        builder.Property(a => a.AllocationRate).HasPrecision(5, 4);
        builder.Property(a => a.AllocatedAmountPhp).HasPrecision(14, 2);
        builder.Property(a => a.PolicyCap).HasPrecision(5, 4);

        builder.Property(a => a.AllocationType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Status).IsRequired().HasMaxLength(50);
        builder.Property(a => a.EvidenceNotes).HasMaxLength(500);

        builder.HasIndex(a => a.DonationId);
        builder.HasIndex(a => a.OtherIncomeId);

        builder.HasOne(a => a.Donation)
            .WithMany(d => d.Allocations)
            .HasForeignKey(a => a.DonationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.OtherIncome)
            .WithMany(i => i.Allocations)
            .HasForeignKey(a => a.OtherIncomeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.SourceFund)
            .WithMany()
            .HasForeignKey(a => a.SourceFundCode)
            .HasPrincipalKey(f => f.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.TargetBucket)
            .WithMany(b => b.Allocations)
            .HasForeignKey(a => a.TargetBucketCode)
            .HasPrincipalKey(b => b.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
