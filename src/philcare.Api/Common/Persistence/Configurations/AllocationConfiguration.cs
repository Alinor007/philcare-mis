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

        builder.Property(a => a.ProgramAmount).HasPrecision(14, 2);
        builder.Property(a => a.AdminAmount).HasPrecision(14, 2);
        builder.Property(a => a.AmilAmount).HasPrecision(14, 2);

        builder.HasIndex(a => a.DonationId).IsUnique();

        builder.HasOne(a => a.Donation)
            .WithOne(d => d.Allocation)
            .HasForeignKey<Allocation>(a => a.DonationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.FundBucket)
            .WithMany(b => b.Allocations)
            .HasForeignKey(a => a.FundBucketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
