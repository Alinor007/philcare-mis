using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using philcare.Api.Features.Finance.Domain;

namespace philcare.Api.Common.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AmountOriginal).HasPrecision(14, 2);
        builder.Property(e => e.FxRateToPhp).HasPrecision(18, 6);
        builder.Property(e => e.AmountPhp).HasPrecision(14, 2);

        builder.Property(e => e.PayeeVendor).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ExpenseCategory).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.FundCode).IsRequired().HasMaxLength(20);
        builder.Property(e => e.ProgramOrProject).HasMaxLength(200);
        builder.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ReceiptNo).HasMaxLength(100);
        builder.Property(e => e.ApprovalStatus).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ApprovedBy).HasMaxLength(200);
        builder.Property(e => e.SupportingDocStatus).HasMaxLength(50);
        builder.Property(e => e.ExpenseFunction).HasMaxLength(100);
        builder.Property(e => e.FundingBucketCode).IsRequired().HasMaxLength(20);
        builder.Property(e => e.ZakatAsnaf).HasMaxLength(50);
        builder.Property(e => e.BeneficiaryType).HasMaxLength(100);
        builder.Property(e => e.AdminEligibility).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.Property(e => e.IsVoided).HasDefaultValue(false);

        builder.HasOne(e => e.Fund)
            .WithMany()
            .HasForeignKey(e => e.FundCode)
            .HasPrincipalKey(f => f.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.FundingBucket)
            .WithMany(b => b.Expenses)
            .HasForeignKey(e => e.FundingBucketCode)
            .HasPrincipalKey(b => b.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
