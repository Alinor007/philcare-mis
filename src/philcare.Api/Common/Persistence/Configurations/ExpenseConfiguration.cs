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

        builder.Property(e => e.Amount).HasPrecision(14, 2);

        builder.Property(e => e.ExpenseCategory).IsRequired().HasMaxLength(50);
        builder.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.Reference).HasMaxLength(100);
        builder.Property(e => e.ZakatAsnaf).HasMaxLength(50);

        builder.Property(e => e.IsVoided).HasDefaultValue(false);

        builder.HasOne(e => e.FundBucket)
            .WithMany(b => b.Expenses)
            .HasForeignKey(e => e.FundBucketId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
