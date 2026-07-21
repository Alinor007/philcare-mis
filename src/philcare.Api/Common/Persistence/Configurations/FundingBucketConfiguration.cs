using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class FundingBucketConfiguration : IEntityTypeConfiguration<FundingBucket>
{
    public void Configure(EntityTypeBuilder<FundingBucket> builder)
    {
        builder.ToTable("FundingBuckets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Code).IsRequired().HasMaxLength(20);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.FundCode).IsRequired().HasMaxLength(20);

        builder.Property(b => b.BucketType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.MaxAdminRate).HasPrecision(5, 4);
        builder.Property(b => b.AllocatedAmount).HasPrecision(14, 2);
        builder.Property(b => b.ExpensedAmount).HasPrecision(14, 2);

        builder.Property(b => b.PolicyRule).HasMaxLength(1000);
        builder.Property(b => b.TypicalUse).HasMaxLength(500);

        builder.Ignore(b => b.Remaining);

        builder.HasIndex(b => b.Code).IsUnique();
    }
}
