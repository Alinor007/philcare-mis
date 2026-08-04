using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class OtherIncomeConfiguration : IEntityTypeConfiguration<OtherIncome>
{
    public void Configure(EntityTypeBuilder<OtherIncome> builder)
    {
        builder.ToTable("OtherIncomes");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.IncomeType).IsRequired().HasMaxLength(50);
        builder.Property(i => i.Source).IsRequired().HasMaxLength(200);

        builder.Property(i => i.AmountOriginal).HasPrecision(14, 2);
        builder.Property(i => i.FxRateToPhp).HasPrecision(18, 6);
        builder.Property(i => i.AmountPhp).HasPrecision(14, 2);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(10);

        builder.Property(i => i.FundCode).IsRequired().HasMaxLength(20);
        builder.Property(i => i.FundingBucketCode).IsRequired().HasMaxLength(20);

        builder.Property(i => i.ReceiptNo).HasMaxLength(100);
        builder.Property(i => i.EvidenceLink).HasMaxLength(500);
        builder.Property(i => i.Notes).HasMaxLength(1000);

        builder.Property(i => i.IsVoided).HasDefaultValue(false);

        builder.HasOne(i => i.Fund)
            .WithMany()
            .HasForeignKey(i => i.FundCode)
            .HasPrincipalKey(f => f.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.FundingBucket)
            .WithMany()
            .HasForeignKey(i => i.FundingBucketCode)
            .HasPrincipalKey(b => b.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
