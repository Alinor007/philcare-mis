using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class FundBucketConfiguration : IEntityTypeConfiguration<FundBucket>
{
    public void Configure(EntityTypeBuilder<FundBucket> builder)
    {
        builder.ToTable("FundBuckets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.FundType).IsRequired().HasMaxLength(50);

        builder.Property(b => b.TotalReceived).HasPrecision(14, 2);
        builder.Property(b => b.AdminAllocated).HasPrecision(14, 2);
        builder.Property(b => b.ProgramAllocated).HasPrecision(14, 2);
        builder.Property(b => b.TotalExpensed).HasPrecision(14, 2);

        builder.Ignore(b => b.Balance);

        builder.HasIndex(b => b.FundType).IsUnique();
    }
}
