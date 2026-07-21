using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.ToTable("Donations");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.AmountOriginal).HasPrecision(14, 2);
        builder.Property(d => d.FxRateToPhp).HasPrecision(18, 6);
        builder.Property(d => d.AmountPhp).HasPrecision(14, 2);

        builder.Property(d => d.AdminRateInput).HasPrecision(5, 4);
        builder.Property(d => d.AdminRateCap).HasPrecision(5, 4);
        builder.Property(d => d.AdminRateApplied).HasPrecision(5, 4);
        builder.Property(d => d.ProgramAllocationPhp).HasPrecision(14, 2);
        builder.Property(d => d.AdminAllocationPhp).HasPrecision(14, 2);

        builder.Property(d => d.Currency).IsRequired().HasMaxLength(10);
        builder.Property(d => d.Channel).IsRequired().HasMaxLength(50);
        builder.Property(d => d.Purpose).HasMaxLength(200);
        builder.Property(d => d.ProgramOrProject).HasMaxLength(200);
        builder.Property(d => d.FundCode).IsRequired().HasMaxLength(20);
        builder.Property(d => d.ReceiptNo).HasMaxLength(100);
        builder.Property(d => d.CashDocumentationStatus).HasMaxLength(50);
        builder.Property(d => d.ComplianceCheck).HasMaxLength(200);
        builder.Property(d => d.ProgramBucketCode).HasMaxLength(20);
        builder.Property(d => d.AdminBucketCode).HasMaxLength(20);
        builder.Property(d => d.AllocationStatus).IsRequired().HasMaxLength(50);
        builder.Property(d => d.Notes).HasMaxLength(1000);

        builder.Property(d => d.KydStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.RiskRating).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(d => d.IsVoided).HasDefaultValue(false);

        builder.HasOne(d => d.Donor)
            .WithMany(don => don.Donations)
            .HasForeignKey(d => d.DonorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Fund)
            .WithMany()
            .HasForeignKey(d => d.FundCode)
            .HasPrincipalKey(f => f.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
